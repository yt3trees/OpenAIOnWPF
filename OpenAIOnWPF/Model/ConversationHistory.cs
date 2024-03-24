using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

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

    public class PromptTemplate
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

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged("Description");
            }
        }

        private string _prompt;
        public string Prompt
        {
            get { return _prompt; }
            set
            {
                _prompt = value;
                OnPropertyChanged("Prompt");
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

        private int _sortOrder;
        public int SortOrder
        {
            get { return _sortOrder; }
            set
            {
                _sortOrder = value;
                OnPropertyChanged("SortOrder");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PromptTemplate()
        {
            ID = Guid.NewGuid();
            CreationDate = DateTime.Now;
            LastUpdated = DateTime.Now;
        }
    }
    public class PromptTemplateManager
    {
        public ObservableCollection<PromptTemplate> Templates { get; set; }
    }

    /// <summary>
    /// VisionAPIのUserMessage内のContent
    /// </summary>
    public class VisionUserContentItem
    {
        public string type { get; set; }
        public string text { get; set; }
        public Image_Url image_url { get; set; }
        //public string Url { get; set; }
    }
    public class Image_Url
    {
        public string url { get; set; }
        public string detail { get; set; }
    }
    /// <summary>
    /// ListBoxのFavorite表示用コンバータ
    /// </summary>
    public class FavoriteToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return "★";
            }
            return "☆";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string && (string)value == "★";
        }
    }
    public class ContentToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ラベルのContentが空でなければVisibility.Visibleを、そうでなければVisibility.Collapsedを返す
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
