using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpStrike
{
    public class EntityPlayer : Entity
    {
        private float _size;
        private Color _color;
        private bool _clickDelay;
        private int delay = 100;
        private bool _shooting;

        public Vector2 PartialPos => lastPos + (pos - lastPos) * Game.Instance.PartialTicks;

        public EntityPlayer(float x, float y, float size, Color color) : base(new Vector2(x, y))
        {
            _size = size;
            _color = color;

            collisionBoundingBox = new AxisAlignedBB(size);
            boundingBox = collisionBoundingBox.Offset(pos - (Vector2.UnitX * collisionBoundingBox.size.X / 2 + Vector2.UnitY * collisionBoundingBox.size.Y / 2));
        }

        public override void Update()
        {
            if (_shooting)
                Shoot();

            base.Update();
        }

        public override void Render(float partialTicks)
        {
            var partialPos = lastPos + (pos - lastPos) * partialTicks;

            GL.Color3(_color);

            partialPos += Vector2.NormalizeFast(motion) * motion.LengthFast * 0.5f;

            GL.Translate(partialPos.X, partialPos.Y, 0);
            GL.Scale(_size, _size, 1);
            GL.Begin(PrimitiveType.Polygon);
            VertexUtil.PutCircle();
            GL.End();
            GL.Scale(1 / _size, 1 / _size, 1);
            GL.Translate(-partialPos.X, -partialPos.Y, 0);
        }

        public void BeginShoot()
        {
            _shooting = true;

            Shoot();
        }

        public void StopShooting()
        {
            _shooting = false;
        }

        private void Shoot()
        {
            if (_clickDelay)
                return;

            _clickDelay = true;

            Task.Run(async () =>
            {
                await Task.Delay(delay);

                _clickDelay = false;
            });
            
            var vec = PartialPos;

            var dir = new Vector2(Game.Instance.MouseLast.X - Game.Instance.Width / 2f, Game.Instance.MouseLast.Y - Game.Instance.Height / 2f).Normalized();

            var landed = Game.Instance.Map.GetShotLandedPosition(vec, dir); //for clientside purposes

            Game.Instance.ClientHandler.SendMessage(ProtocolType.Udp, "playerShot", vec.X.ToSafeString(), vec.Y.ToSafeString(), dir.X.ToSafeString(), dir.Y.ToSafeString());

            Game.Instance.SpawnEffect(new BulletTraceFX(PartialPos, landed, 2));
        }
    }
}