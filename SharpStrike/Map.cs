﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SharpStrike
{
    public class Map
    {
        private ConcurrentDictionary<Guid, EntityPlayerRemote> _players = new ConcurrentDictionary<Guid, EntityPlayerRemote>();

        private readonly Stopwatch _interpolationTimer = new Stopwatch();

        private List<AxisAlignedBB> _collisionBoxes = new List<AxisAlignedBB>();

        public void LoadBBs(List<AxisAlignedBB> boxes)
        {
            _collisionBoxes = boxes;
        }

        public void SyncPlayers(List<Tuple<Guid, Vector2, Vector2, float>> data)
        {
            foreach (var player in _players.Values)
            {
                player.Update();
            }

            foreach (var tuple in data)
            {
                if (Equals(tuple.Item1, Game.Instance.ClientHandler.Id))
                {
                    Game.Instance.Player.Health = tuple.Item4;
                    continue;
                }

                var ep = _players.GetOrAdd(tuple.Item1, new EntityPlayerRemote(tuple.Item2, 40));
                ep.MoveTo(tuple.Item2);
                ep.Rotation = tuple.Item3;
                ep.Health = tuple.Item4;
            }

            _interpolationTimer.Restart();
        }

        public void RemovePlayer(Guid id)
        {
            if(_players.TryRemove(id, out var removed))
            {
                Console.WriteLine($"Player {id} has disconnected.");
            }
        }

        public List<AxisAlignedBB> GetCollidingBoxes(AxisAlignedBB box)
        {
            var bb = box.Union(box);

            return _collisionBoxes.Where(cb => cb.IntersectsWith(bb)).ToList();
        }

        public void Render(float partialTicks)
        {
            GL.Color3(1, 1, 1f);

            var tex = TextureManager.GetOrRegister("wall");
            GL.BindTexture(TextureTarget.Texture2D, tex);

            for (var index = 0; index < _collisionBoxes.Count; index++)
            {
                var box = _collisionBoxes[index];

                var center = box.GetCenter();

                GL.Translate(center.X, center.Y, 0);
                GL.Scale(box.Size.X, box.Size.Y, 1);

                GL.Begin(PrimitiveType.Quads);
                VertexUtil.PutQuad();
                GL.End();

                GL.Scale(1 / box.Size.X, 1 / box.Size.Y, 1);
                GL.Translate(-center.X, -center.Y, 0);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void RenderRemotePlayers()
        {
            var partialTicks = (float)Math.Min(1, _interpolationTimer.Elapsed.TotalMilliseconds / (Game.Instance.TargetUpdatePeriod * 1000));

            foreach (var player in _players.Values)
            {
                if (player.IsAlive)
                    player.Render(partialTicks);
            }
        }

        public void RenderShadows(Vector2 viewingPos)
        {
            GL.PushAttrib(AttribMask.ColorBufferBit);

            GL.ClearColor(0, 0, 0, 0); //important
            Game.Instance.ShadowFbo.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Color4(0, 0, 0, 1f); //shadow color
            GL.Disable(EnableCap.Blend);
            GL.Begin(PrimitiveType.Quads);

            //render shadows
            var dist = (float)Math.Sqrt(Game.Instance.Width * Game.Instance.Width +
                                        Game.Instance.Height * Game.Instance.Height);
            for (var i = 0; i < _collisionBoxes.Count; i++)
            {
                var box = _collisionBoxes[i];

                for (var index = 0; index < 4; index++)
                {
                    //if last index, this is the first point
                    var pointNext = box[index == 3 ? 0 : index + 1];
                    var point = box[index];

                    //sum of dat magic
                    if (IsLeft(pointNext, point, viewingPos))
                    {
                        GL.Vertex2(point);

                        var dir = Vector2.Normalize(point - viewingPos);
                        var projectedPoint = point + dir * dist;
                        GL.Vertex2(projectedPoint);

                        dir = Vector2.Normalize(pointNext - viewingPos);
                        projectedPoint = pointNext + dir * dist;

                        GL.Vertex2(projectedPoint);
                        GL.Vertex2(pointNext);
                    }
                }
            }

            GL.End();
            GL.PopAttrib();
            Game.Instance.ShadowFbo.Unbind();
            Game.Instance.ShadowFbo.BindTexture();

            GL.Enable(EnableCap.Blend);

            var w = Game.Instance.Width;
            var h = Game.Instance.Height;

            GL.Color4(1, 1, 1, 0.875f);

            GL.Translate(viewingPos.X, viewingPos.Y, 0);
            GL.Scale(w, -h, 1);
            GL.Begin(PrimitiveType.Quads);
            VertexUtil.PutQuad();
            GL.End();
            GL.Scale(1f / w, -1f / h, 1);
            GL.Translate(-viewingPos.X, -viewingPos.Y, 0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        #region deprecated

        /*
        private List<Vector2> CreateShadowPolygon(Vector2 viewer, AxisAlignedBB box)
        {
            List<Vector2> newShape = new List<Vector2>();

            var dist = (float)Math.Sqrt(Game.Instance.Width * Game.Instance.Width + Game.Instance.Height * Game.Instance.Height);

            for (var index = 0; index < 4; index++)
            {
                //if last index, this is the first point
                var pointNext = box[index == 3 ? 0 : index + 1];
                var point = box[index];

                //sum of dat magic
                if (!IsLeft(pointNext, point, viewer))
                {
                    newShape.Add(point);

                    var dir = Vector2.Normalize(point - viewer);
                    var projectedPoint = point + dir * dist;
                    newShape.Add(projectedPoint);

                    dir = Vector2.Normalize(pointNext - viewer);
                    projectedPoint = pointNext + dir * dist;

                    newShape.Add(projectedPoint);
                    newShape.Add(pointNext);
                }
            }

            return newShape;
        }*/

        #endregion deprecated

        public bool IsLeft(Vector2 a, Vector2 b, Vector2 c)
        {
            var val = (c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X);
            return val > 0;
        }

        public Vector2 GetShotLandedPosition(Vector2 pos, Vector2 dir)
        {
            var lastDist = Single.MaxValue;

            foreach (var box in _collisionBoxes)
            {
                if (RayHelper.Intersects(box, pos, dir, out var hit) && hit < lastDist)
                    lastDist = hit;
            }

            return lastDist * dir;
        }
    }
}