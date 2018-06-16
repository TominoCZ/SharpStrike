using OpenTK;

namespace SharpStrike
{
    public class EntityFX : Entity
    {
        public int Age;
        public int MaxAge;

        protected EntityFX(Vector2 pos, int maxAge) : base(pos)
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