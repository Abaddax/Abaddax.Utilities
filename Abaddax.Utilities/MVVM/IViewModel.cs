using System.ComponentModel;

namespace Abaddax.MVVM
{
    /// <summary>
    /// UI-Logic
    /// </summary>
    public interface IViewModel<TModel> : INotifyPropertyChanged where TModel : IModel
    {
        /// <summary>
        /// Data (UI update on change)
        /// </summary>
        TModel Data { get; }
    }
}
