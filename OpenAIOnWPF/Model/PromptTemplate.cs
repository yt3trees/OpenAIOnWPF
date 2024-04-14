using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenAIOnWPF.Model
{
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

}
