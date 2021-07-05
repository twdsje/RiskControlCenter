using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiskCenterProject.RiskRules
{
    class DailyLossLimit : RiskRule
    {
        private double _realized = 0;

        public DailyLossLimit(Account a)
        {
            LimitType = LimitType.Dollars;
            Value = 1000;
            Display = "Uninitalized";

            _realized = a.Get(AccountItem.RealizedProfitLoss, Currency.UsDollar);
            double unrealized = a.Get(AccountItem.UnrealizedProfitLoss, Currency.UsDollar);
            Current = _realized + unrealized;
            Display = "Daily Loss Limit: " + Current + " / " + Value;

            Consequence = ViolationType.Critical;
            Message = "You have reached your daily risk limit.";
        }

        public override bool Calculate(AccountItemEventArgs e)
        {
            if (e.AccountItem == AccountItem.RealizedProfitLoss)
            {
                _realized = e.Value;
            }

            if (e.AccountItem == AccountItem.UnrealizedProfitLoss)
            {
                Current = _realized + e.Value;
                Display =  "Daily Loss Limit: " + Current + " / " + Value;
            }

            if (Current < Value)
            {
                return true;
            }

            return false;
        }
    }
}
