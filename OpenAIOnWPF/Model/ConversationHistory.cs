using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenAIOnWPF.Model
{
    public class ConversationHistory : INotifyPropertyChanged
    {
        public Guid ID { get; set; }
        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        private ObservableCollection<ChatMessage> _messages;
        public ObservableCollection<ChatMessage> Messages
        {
            get { return _messages; }
            set
            {
                if (_messages != value)
                {
                    _messages = value;
                    _messages.CollectionChanged += Messages_CollectionChanged;
                    OnPropertyChanged("Messages");
                }
            }
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    OnPropertyChanged("LastUpdated");
                }
            }
        }

        public bool _favorite;
        public bool Favorite
        {
            get { return _favorite; }
            set
            {
                if (_favorite != value)
                {
                    _favorite = value;
                    OnPropertyChanged("Favorite");
                }
            }
        }
        public DateTime CreationDate { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConversationHistory()
        {
            ID = Guid.NewGuid();
            CreationDate = DateTime.Now;
            LastUpdated = DateTime.Now;
            Messages = new ObservableCollection<ChatMessage>();
        }

        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LastUpdated = DateTime.Now;
        }

        // PropertyChangedイベントを発火するメソッド
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ConversationManager
    {
        public ObservableCollection<ConversationHistory> Histories { get; set; }
    }
}
