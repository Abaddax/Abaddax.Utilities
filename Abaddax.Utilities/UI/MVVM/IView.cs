namespace Abaddax.Utilities.UI.MVVM
{
    /// <summary>
    /// UI-Element
    /// </summary>
    /// <remarks>Add DataContext="{Binding RelativeSource={RelativeSource Self}}" to view to access VM</remarks>
    public interface IView<TViewModel, TModel> where TViewModel : IViewModel<TModel> where TModel : IModel
    {
        /// <summary>
        /// DataBinding
        /// </summary>
        TViewModel VM { get; }
    }
}
