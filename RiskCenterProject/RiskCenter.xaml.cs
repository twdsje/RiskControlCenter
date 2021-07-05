using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using RiskCenterProject.RiskRules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace RiskCenterProject
{
	/// <summary>
	/// Interaction logic for Page1.xaml
	/// </summary>
	public partial class RiskCenter
	{
		#region Variables
		// RiskCenterFramework SECTION
		private RiskCenterFrameworkDisplay outputType;
		public enum RiskCenterFrameworkDisplay          // enum to help determine what to display in our output box
		{
			acctExec,
			acctOrders,
			acctPos,
			acctStrat,
			acctValues,
			buyMarket,
			connectionInfo,
			connectKinetickEOD,
			frameworkManaged,
			fundamentalData,
			marketData,
			marketDataSnapshot,
			marketDepthAsk,
			marketDepthBid,
			onAccountItemUpdate,
			onAccountStatusUpdate,
			onConnectionStatusUpdate,
			onExecutionUpdate,
			onNews,
			onOrderUpdate,
			onPositionUpdate,
			onSimulationAccountReset,
			requestData,
			realtimeData,
			sellMarket,
		}

		// private int                          barCount = 0;               // Used if processing real-time bars only on bar closes
		private NinjaTrader.Cbi.Instrument instrument;
		private bool barsRequestSubscribed = false;
		private NinjaTrader.Data.BarsRequest barsRequest;
		private NinjaTrader.Data.MarketData marketData;
		private NinjaTrader.Data.MarketDepth<NinjaTrader.Data.MarketDepthRow> marketDepth;
		private NinjaTrader.Data.FundamentalData fundamentalData;
		private NinjaTrader.Data.NewsSubscription newsSubscription;
		private NinjaTrader.Data.NewsItems newsItems;

		private Connection connection;


		private Order stopLoss;
		private Order profitTarget;
		private Order frameworkEntryOrder;
		private Order entryOrder;

		private Dictionary<string, ObservableCollection<RiskRule>> AccountRules;
		public ObservableCollection<RiskRule> CurrentAccountRules { get; set; }

		#endregion
		public RiskCenter()
		{

			DataContext = this;
			InitializeComponent();
			// Sets the tab header name to be the currently selected instrument's name
			TabName = "@INSTRUMENT_FULL";

			// Subscribe to account status updates. This event will fire as connection.Status of the hosting connection changes
			Account.AccountStatusUpdate += OnAccountStatusUpdate;

			AccountRules = new Dictionary<string,ObservableCollection<RiskRule>>();


			accountSelector.SelectionChanged += (o, args) =>
			{
				//Todo: what to display when no account.
				if (accountSelector.SelectedAccount != null)
				{
					// Unsubscribe to any prior account subscriptions
					accountSelector.SelectedAccount.AccountItemUpdate -= OnAccountItemUpdate;
					accountSelector.SelectedAccount.PositionUpdate -= OnPositionUpdate;

					// Subscribe to new account subscriptions
					accountSelector.SelectedAccount.AccountItemUpdate += OnAccountItemUpdate;
					accountSelector.SelectedAccount.PositionUpdate += OnPositionUpdate;
					ObservableCollection<RiskRule> r;

					if (!AccountRules.TryGetValue(accountSelector.SelectedAccount.Name, out r))
					{
						CurrentAccountRules = new ObservableCollection<RiskRule>();
						AccountRules.Add(accountSelector.SelectedAccount.Name, CurrentAccountRules);
						OnPropertyChanged("CurrentAccountRules");

						CurrentAccountRules.Add(new DailyLossLimit(accountSelector.SelectedAccount));
					}
					else
					{
						CurrentAccountRules = r;
					}
				}
			};
		}

		private void OnButtonClick(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			if (button != null && 
				(ReferenceEquals(button, addRuleButton) || ReferenceEquals(button, addRuleButton2)))
			{

				CurrentAccountRules.Add(new DailyLossLimit(accountSelector.SelectedAccount));
			}
		}

		AccountItemEventArgs myArgs;

        

        private void DoWarning(RiskRule r)
		{
			//show the popup.
			//r.GetWarningMessage();
		}

		#region Methods for Account Section
		// This method is fired on any change of an 'Account Value'
		private void OnAccountItemUpdate(object sender, AccountItemEventArgs e)
		{
			try
			{
				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Account: {1}{0}AccountItem: {2}{0}Value: {3}",
						Environment.NewLine,
						e.Account.Name,
						e.AccountItem,
						e.Value));

					if (e.AccountItem == AccountItem.UnrealizedProfitLoss ||
					e.AccountItem == AccountItem.RealizedProfitLoss)
					{
						bool noViolations = true;

						foreach (RiskRule r in CurrentAccountRules)
						{
							if (!r.Calculate(e))
							{
								noViolations = false;

								switch (r.Consequence)
								{
									default:
										DoWarning(r);
										return;
								}

							}
						}
					}

				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "RiskCenterFramework - OnAccountItemUpdate Exception: " + error.ToString();
				});
			}
		}

		// This method is fired on any status change of any account
		private void OnAccountStatusUpdate(object sender, AccountStatusEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != RiskCenterFrameworkDisplay.onAccountStatusUpdate)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				Dispatcher.InvokeAsync(() =>
				{
					outputBox.AppendText(string.Format("{0}Account: {1}{0}Status: {2}",
						Environment.NewLine,
						e.Account.Name,
						e.Status));
				});
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "RiskCenterFramework - OnAccountStatusUpdate Exception: " + error.ToString();
				});
			}
		}
		#endregion

		// This method is fired as a position changes
		private void OnPositionUpdate(object sender, PositionEventArgs e)
		{
			try
			{
				// Only display messages from this method if our output box is displaying this category of messages
				if (outputType != RiskCenterFrameworkDisplay.onPositionUpdate)
					return;

				/* Dispatcher.InvokeAsync() is needed for multi-threading considerations. When processing events outside of the UI thread, and we want to
                influence the UI .InvokeAsync() allows us to do so. It can also help prevent the UI thread from locking up on long operations. */
				//                Dispatcher.InvokeAsync(() =>
				//                {
				//                    outputBox.AppendText(string.Format("{0}Instrument: {1}{0}MarketPosition: {2}{0}Quantity: {3}{0}AveragePrice: {4}{0}Operation: {5}{0}UnrealizedPnL: {6}{0}Connection: {7}{0}",
				//                        Environment.NewLine,
				//                        e.Position.Instrument.FullName,
				//                        e.MarketPosition,
				//                        e.Quantity,
				//                        e.AveragePrice,
				//                        e.Operation,
				//                        (e.Position.Instrument.MarketData.Last != null ? e.Position.GetProfitLoss(e.Position.Instrument.MarketData.Last.Price, PerformanceUnit.Currency).ToString() : "(unknown)"),
				//                        accountSelector.SelectedAccount.Connection.Options.Name));
				//                });
			}
			catch (Exception error)
			{
				Dispatcher.InvokeAsync(() =>
				{
					// It is important to protect NinjaTrader from any unhandled exceptions that may arise from your code
					outputBox.Text = "RiskCenterFramework - OnPositionUpdate Exception: " + error.ToString();
				});
			}
		}

		// NTTabPage member. Required to determine the text for the tab header name
		protected override string GetHeaderPart(string variable)
		{
			return "Risk Center";
		}

		// Called by TabControl when tab is being removed or window is closed
		public override void Cleanup()
		{
			// Unsubscribe and cleanup any remaining resources we may still have open
			Account.AccountStatusUpdate -= OnAccountStatusUpdate;
			if (accountSelector.SelectedAccount != null)
			{
				accountSelector.SelectedAccount.AccountItemUpdate -= OnAccountItemUpdate;
				accountSelector.SelectedAccount.PositionUpdate -= OnPositionUpdate;
			}

			// a call to base.Cleanup() will loop through the visual tree looking for all ICleanable children
			// i.e., AccountSelector, AtmStrategySelector, InstrumentSelector, IntervalSelector, TifSelector,
			// as well as unregister any link control events
			base.Cleanup();
		}

		// NTTabPage member. Required for restoring elements from workspace
		protected override void Restore(XElement element)
		{
			if (element == null)
				return;

			// Restore the previously selected account
			XElement accountElement = element.Element("Account");
			if (accountSelector != null && accountElement != null)
				accountSelector.DesiredAccount = accountElement.Value;
		}

		// NTTabPage member. Required for storing elements to workspace
		protected override void Save(XElement element)
		{
			if (element == null)
				return;

			// Save the currently selected account
			if (accountSelector != null && !string.IsNullOrEmpty(accountSelector.DesiredAccount))
				element.Add(new XElement("Account") { Value = accountSelector.DesiredAccount });
		}
	}
}
