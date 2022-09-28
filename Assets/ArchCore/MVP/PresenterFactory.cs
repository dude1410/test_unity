using Zenject;

namespace ArchCore.MVP
{
    public class PresenterFactory
    {
        private DiContainer diContainer;

        public PresenterFactory(DiContainer diContainer)
        {
            this.diContainer = diContainer;
        }

        public T Create<T>() where T : Presenter
        {
            return diContainer.Instantiate<T>();
        }
    }
}