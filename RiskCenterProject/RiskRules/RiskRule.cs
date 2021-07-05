using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiskCenterProject.RiskRules
{

    public enum LimitType
    { Dollars, Ticks, Points, Percentage }

    public enum ViolationType
    { Warn, Critical}

    public class RiskRule : INotifyPropertyChanged
    {
        private LimitType _limitType;
        private double _value;
        private double _current;
        private ViolationType _consequence;
        private string _message;
        private string _display;

        public RiskRule()
        {

        }

        public virtual bool Calculate(AccountItemEventArgs e)
        {
            return true;
        }



        public KeyValuePair<string, string> Options;

        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

        public double Current
        {
            get
            {
                return _current;
            }
            set
            {
                _current = value;
                OnPropertyChanged("Current");
            }
        }

        public LimitType LimitType
        {
            get
            {
                return _limitType;
            }
            set
            {
                _limitType = value;
                OnPropertyChanged("LimitType");
            }
        }

        public ViolationType Consequence
        {
            get
            {
                return _consequence;
            }
            set
            {
                _consequence = value;
                OnPropertyChanged("Consequence");
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        public string Display
        {
            get
            {
                return _display;
            }
            set
            {
                _display = value;
                OnPropertyChanged("Display");
            }
        }

        public Array LimitTypeValues => Enum.GetValues(typeof(LimitType));
        public Array ViolationTypeValues => Enum.GetValues(typeof(ViolationType));

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
