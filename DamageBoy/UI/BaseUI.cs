
namespace DamageBoy.UI
{
    abstract class BaseUI
    {
        protected bool isVisible;
        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; }
        }

        bool lastIsVisible = false;

        public BaseUI()
        {

        }

        public void Render()
        {
            if (lastIsVisible != IsVisible)
            {
                lastIsVisible = IsVisible;
                VisibilityChanged(IsVisible);
            }

            if (!IsVisible) return;

            InternalRender();
        }

        protected virtual void VisibilityChanged(bool isVisible) { }
        protected abstract void InternalRender();
    }
}