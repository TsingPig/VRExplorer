namespace VRAgent
{
    public interface ButtonEntity : BaseEntity
    {
        public enum ButtonState
        {
            Pressed,
            Released
        }

        void OnPress();

        void OnRelease();
    }
}