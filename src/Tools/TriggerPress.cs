using System;

namespace phosphorite
{
    public class TriggerPress
    {
        bool active, down, armed;
        public void Reset()
        {
            active = false;
            down = false;
            armed = false;
        }

        public bool Step(bool aiming, float trigger, bool validTarget)
        {
            if (!aiming || !LightValues.IsFinite(trigger))
            {
                Reset();
                return false;
            }
            if (!active)
            {
                active = true;
                down = trigger >= 0.65f;
                armed = !down;
                return false;
            }
            if (trigger <= 0.25f)
            {
                down = false;
                armed = true;
                return false;
            }
            if (trigger >= 0.65f && !down)
            {
                down = true;
                bool fire = armed && validTarget;
                armed = false;
                return fire;
            }
            return false;
        }
    }
}
