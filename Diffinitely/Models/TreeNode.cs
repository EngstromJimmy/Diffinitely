namespace Diffinitely.Models
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.Extensibility;
    using Microsoft.VisualStudio.Extensibility.UI;

    [DataContract]
    internal class TreeNode : INotifyPropertyChanged
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public ImageMoniker Icon { get; set; }

        [DataMember]
        public int CommentCount { get; set; }
        [DataMember]
        public ObservableCollection<TreeNode> Children { get; } = [];

        private bool _isExpanded;
        [DataMember]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    RaisePropertyChanged();
                }
            }
        }
        [DataMember]
        public string? FullPath { get; set; }

        [DataMember]
        public IAsyncCommand? OpenCommand { get; set; }

        [DataMember]
        public IAsyncCommand? OpenCommentsCommand { get; set; }
        [DataMember]
        public string? ChangeKind { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
