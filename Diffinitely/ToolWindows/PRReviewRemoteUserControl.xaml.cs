using Diffinitely.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace Diffinitely.ToolWindows
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    [DataContract]
    internal class TreeNode : INotifyPropertyChanged
    {
        public TreeNode(string name, IEnumerable<TreeNode>? children = null)
        {
            Name = name;

            if (children != null)
            {
                foreach (var c in children)
                    Children.Add(c);
            }
        }

        // text shown in the tree
        [DataMember]
        public string Name { get; set; }

        // child nodes
        [DataMember]
        public ObservableCollection<TreeNode> Children { get; }
            = new ObservableCollection<TreeNode>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    internal class PRReviewRemoteUserControl : RemoteUserControl
    {
        private readonly GitHubPullRequestService _prService = new GitHubPullRequestService();
        private readonly VisualStudioExtensibility _extensibility;
        public PRReviewViewModel ViewModel { get; internal set; }
        public PRReviewRemoteUserControl(VisualStudioExtensibility extensibility) : base(dataContext: new PRReviewViewModel())
        {
            _extensibility = extensibility;
            ViewModel = (PRReviewViewModel)this.DataContext;
        }
    }
}
