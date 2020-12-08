namespace xmaslights
{
    public interface ILight
    {
        void Off();

        void On();

        void Switch();

        void Rotate(int angle);

        bool IsOn();

        void Click();
    }
}
