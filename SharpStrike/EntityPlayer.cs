using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpStrike
{
    public class EntityPlayer : Entity
    {
        private float _size;
        private bool _clickDelay;
        private int _delay = 100;
        private bool _shooting;

        public float Health = 100;

        public Vector2 Rotation;

        public override bool IsAlive => Health > 0;

        public Vector2 PartialPos => LastPos + (Pos - LastPos) * Game.Instance.PartialTicks;

        public EntityPlayer(float x, float y, float size, Color color) : base(new Vector2(x, y))
        {
            _size = size;

            CollisionBoundingBox = new AxisAlignedBB(size);
            BoundingBox = CollisionBoundingBox.Offset(Pos - (Vector2.UnitX * CollisionBoundingBox.Size.X / 2 + Vector2.UnitY * CollisionBoundingBox.Size.Y / 2));
        }

        public override void Update()
        {
            if (_shooting)
                Shoot();

            base.Update();
        }

        public override void Render(float partialTicks)
        {
            if (!IsAlive)
                return;

            var partialPos = LastPos + (Pos - LastPos) * partialTicks;

            GL.Color3(1, 1, 1f);

            GL.BindTexture(TextureTarget.Texture2D, TextureManager.GetOrRegister("red"));

            var x = Game.Instance.MouseLast.X - Game.Instance.Width / 2f;
            var y = Game.Instance.MouseLast.Y - Game.Instance.Height / 2f;

            Rotation = Vector2.Normalize(new Vector2(x, y));

            var ratio = Rotation.X == 0 ? (Rotation.Y > 0 ? 90 : -90) : Rotation.Y / Rotation.X;

            var atan = Math.Atan(float.IsNaN(ratio) || float.IsInfinity(ratio) ? 0 : ratio);
            var angle = 90 + (float)MathHelper.RadiansToDegrees(Rotation.X < 0 ? atan + MathHelper.Pi : atan);

            GL.Translate(partialPos.X, partialPos.Y, 0);
            GL.Scale(_size, _size, 1);
            GL.Rotate(angle, 0, 0, 1);
            GL.Begin(PrimitiveType.Quads);
            VertexUtil.PutQuad();
            GL.End();
            GL.Rotate(angle, 0, 0, -1);
            GL.Scale(1 / _size, 1 / _size, 1);
            GL.Translate(-partialPos.X, -partialPos.Y, 0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void BeginShoot()
        {
            if (!IsAlive)
                return;

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
                await Task.Delay(_delay);

                _clickDelay = false;
            });

            var vec = PartialPos;

            var dir = new Vector2(Game.Instance.MouseLast.X - Game.Instance.Width / 2f, Game.Instance.MouseLast.Y - Game.Instance.Height / 2f).Normalized();

            var landed = Game.Instance.Map.GetShotLandedPosition(vec, dir); //for clientside purposes

            var payload = new ByteBufferWriter(2);
            payload.WriteGuid(Game.Instance.ClientHandler.Id);
            payload.WriteVec2(vec);
            payload.WriteVec2(dir);

            Game.Instance.ClientHandler.SendMessage(ProtocolType.Udp, payload);

            Game.Instance.SpawnEffect(new BulletTraceFx(PartialPos, landed, 3));
        }
    }
}