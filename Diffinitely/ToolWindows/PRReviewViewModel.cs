using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
namespace Diffinitely.ToolWindows;

[DataContract]
internal class PRReviewViewModel : INotifyPropertyChanged
{
    public PRReviewViewModel()
    {
        // Build some demo data
        Roots.Add(
            new TreeNode("Project A", new[]
            {
            new TreeNode("Controllers", new []
            {
                new TreeNode("WeatherController.cs"),
                new TreeNode("AuthController.cs"),
            }),
            new TreeNode("Models", new []
            {
                new TreeNode("User.cs"),
                new TreeNode("WeatherForecast.cs"),
            }),
            new TreeNode("Program.cs"),
            new TreeNode("appsettings.json"),
            })
        );

        Roots.Add(
            new TreeNode("Project B (Tests)", new[]
            {
            new TreeNode("UnitTests", new []
            {
                new TreeNode("WeatherTests.cs"),
                new TreeNode("AuthTests.cs"),
            }),
            new TreeNode("TestBase.cs"),
            })
        );
    }

    // top-level nodes in the TreeView
    [DataMember]
    public ObservableCollection<TreeNode> Roots { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged(string propName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}
