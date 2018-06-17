using OpenTK;

namespace SharpStrike
{
    public class EntityFx : Entity
    {
        public int Age;
        public int MaxAge;

        protected EntityFx(Vector2 pos, int maxAge) : base(pos)
        {
            MaxAge = maxAge;
        }

        public override void Update()
        {
            if (Age++ >= MaxAge)
                IsAlive = false;

            base.Update();
        }
    }
}