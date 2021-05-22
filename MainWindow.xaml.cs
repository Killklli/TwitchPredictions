using System;
using System.Collections.Generic;
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
using TwitchLib.Api.Helix.Models.Predictions;
using TwitchLib.Api;
using System.Threading;
using Grapevine.Server;
using Grapevine.Server.Attributes;
using Grapevine.Interfaces.Server;
using System.Diagnostics;
using Microsoft.Win32;
using System.Web;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;

namespace IronmonPredictions
{
    static class OauthToken
    {
        public static string token = "";
    }
    [RestResource]
    public class OauthPassthrough
    {
        [RestRoute]
        public IHttpContext Oauth(IHttpContext context)
        {
            if (context.Request.HttpMethod == Grapevine.Shared.HttpMethod.POST)
            {
                string input = context.Request.Payload;
                string output = input.Split("access_token=", '&')[1].Split('&')[0];
                OauthToken.token = output;
            }
            context.Response.SendResponse("<html><script>var hash = window.location.hash.substr(1); var xhr = new XMLHttpRequest(); xhr.open(\"POST\", \"/\", true); xhr.setRequestHeader(\"Content-Type\", \"application/x-www-form-urlencoded; charset=UTF-8\"); xhr.send(hash); setTimeout(function(){window.close();},3000);</script>You may now close this window.</html>");
            return context;
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private TwitchAPI API;
        private string user = "";
        private readonly List<Prediction> predictions = new List<Prediction>();
        private readonly RestServer server = new Grapevine.Server.RestServer();
        private readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Purple");
            server.Start();
            using (StreamReader sr = File.OpenText("./predictions.json"))
            {
                predictions = JsonConvert.DeserializeObject<List<Prediction>>(sr.ReadToEnd());
            }
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            var gridView = new GridView();
            Titles.View = gridView;
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Title",
                DisplayMemberBinding = new Binding("Title"),
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Option 1",
                DisplayMemberBinding = new Binding("Option1"),
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Option 2",
                DisplayMemberBinding = new Binding("Option2"),
            });
            foreach (var item in predictions)
            {
                Titles.Items.Add(item);
            }
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (OauthToken.token != "")
            {
                try
                {
                    AuthButton.IsEnabled = false;
                    Titles.IsEnabled = true;
                    if (API is null)
                    {
                        if (OauthToken.token != "")
                        {
                            API = new TwitchAPI();
                            API.Settings.ClientId = "k2xx6ch0yzfnc3a2708rir5079darf";
                            API.Settings.AccessToken = OauthToken.token;
                            user = API.V5.Users.GetUserAsync().Result.Id;
                            get_current_prediction();
                        }
                    }
                    else
                    {
                        get_current_prediction();
                    }
                }
                catch { }
            }

        }
        private void get_current_prediction()
        {
            var resp = API.Helix.Predictions.GetPredictions(user).Result;
            if (resp.Data != null)
            {
                var latest = resp.Data[0];
                if (latest.WinningOutcomeId == null)
                {
                    if ((string)Timer.Content != "00:00:00")
                    {
                        if ((string)CurrentPredictionTitle.Content != latest.Title)
                        {
                            _timer.Start();
                        }
                        CurrentPredictionTitle.Content = latest.Title;
                        var BlueId = latest.Outcomes.Where(x => x.Color.ToLower() == "blue").First();
                        var PinkId = latest.Outcomes.Where(x => x.Color.ToLower() == "pink").First();
                        BlueEntrants.Content = BlueId.ChannelPoints;
                        BluePoints.Content = BlueId.ChannelPointsVotes;
                        PinkEntrants.Content = PinkId.ChannelPoints;
                        PinkPoints.Content = PinkId.ChannelPointsVotes;

                        Option1Title.Content = BlueId.Title;
                        Option2Title.Content = PinkId.Title;
                        int total = BlueId.ChannelPointsVotes + PinkId.ChannelPointsVotes;
                        int BluePercent = 0;
                        int PinkPercent = 0;
                        if (total != 0)
                        {
                            int BluePercentTemp = (int)Math.Round((double)(BlueId.ChannelPointsVotes / total) * 100);
                            int PinkPercentTemp = (int)Math.Round((double)(PinkId.ChannelPointsVotes / total) * 100);
                            if (BluePercentTemp <= 0)
                            {
                                BluePercent = 0;
                            }
                            else
                            {
                                BluePercent = BluePercentTemp;
                            }
                            if (PinkPercentTemp <= 0)
                            {
                                PinkPercent = 0;
                            }
                            else
                            {
                                PinkPercent = PinkPercentTemp;
                            }
                        }
                        Option1Percent.Content = BluePercent + "%";
                        Option2Percent.Content = PinkPercent + "%";
                        BlueWins.IsEnabled = true;
                        PinkWins.IsEnabled = true;
                        ClosePredButton.IsEnabled = true;
                        CancelPredButton.IsEnabled = true;
                    }
                }
                else
                {
                    BlueWins.IsEnabled = false;
                    PinkWins.IsEnabled = false;
                    ClosePredButton.IsEnabled = false;
                    CancelPredButton.IsEnabled = false;

                    CurrentPredictionTitle.Content = "None";
                    BlueEntrants.Content = "0";
                    BluePoints.Content = "0";
                    PinkEntrants.Content = "0";
                    PinkPoints.Content = "0";
                    Option1Title.Content = "None";
                    Option2Title.Content = "None";

                }
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            server.Stop();
        }


        private void Authenticate(object sender, RoutedEventArgs e)
        {
            string target = "https://api.twitch.tv/kraken/oauth2/authorize?response_type=token&client_id=k2xx6ch0yzfnc3a2708rir5079darf&redirect_uri=http://localhost:1234&scope=channel:manage:predictions%20channel:read:predictions%20user_read%20channel_read";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                MessageBox.Show("Something went wrong, please directly load " + target);
            }
        }

        private void BlueWin(object sender, RoutedEventArgs e)
        {
            var resp = API.Helix.Predictions.GetPredictions(user).Result;
            if (resp.Data != null)
            {
                var latest = resp.Data[0];
                var BlueId = latest.Outcomes.Where(x => x.Color.ToLower() == "blue").First();
                try
                {
                    API.Helix.Predictions.EndPrediction(user, latest.Id, TwitchLib.Api.Core.Enums.PredictionStatusEnum.RESOLVED, BlueId.Id);
                    try
                    {
                        _timer.Stop();
                    }
                    catch { }
                    Timer.Content = "00:00:00";
                }
                catch (TwitchLib.Api.Core.Exceptions.BadRequestException) { }
            }
        }

        private void PinkWin(object sender, RoutedEventArgs e)
        {
            var resp = API.Helix.Predictions.GetPredictions(user).Result;
            if (resp.Data != null)
            {
                var latest = resp.Data[0];
                var PinkId = latest.Outcomes.Where(x => x.Color.ToLower() == "pink").First();
                try
                {
                    API.Helix.Predictions.EndPrediction(user, latest.Id, TwitchLib.Api.Core.Enums.PredictionStatusEnum.RESOLVED, PinkId.Id);
                    _timer.Stop();
                    Timer.Content = "00:00:00";
                }
                catch (TwitchLib.Api.Core.Exceptions.BadRequestException) { }
            }
        }

        private void ClosePrediction(object sender, RoutedEventArgs e)
        {
            var resp = API.Helix.Predictions.GetPredictions(user).Result;
            if (resp.Data != null)
            {
                var latest = resp.Data[0];
                try
                {
                    API.Helix.Predictions.EndPrediction(user, latest.Id, TwitchLib.Api.Core.Enums.PredictionStatusEnum.CANCELED);
                    ClosePredButton.IsEnabled = false;
                    try
                    {
                        _timer.Stop();
                    }
                    catch { }
                    Timer.Content = "00:00:00";
                }
                catch (TwitchLib.Api.Core.Exceptions.BadRequestException) { }

            }
        }
        void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = Titles.SelectedItem;
            if (item != null)
            {
                var casted = (Prediction)item;
                create_prediction(casted.Title, casted.Duration, casted.Option1, casted.Option2);
            }
        }
        private void create_prediction(string Title, int Time, string Option1, string Option2)
        {
            List<TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome> outcomes = new List<TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome>
                {
                    new TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome
                    {
                        Title = Option1
                    },
                    new TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.Outcome
                    {
                        Title = Option2
                    }
                };
            TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.CreatePredictionRequest newpred = new TwitchLib.Api.Helix.Models.Predictions.CreatePrediction.CreatePredictionRequest
            {
                BroadcasterId = user,
                Title = Title,
                PredictionWindowSeconds = Time,
                Outcomes = outcomes.ToArray()
            };
            try
            {
                API.Helix.Predictions.CreatePrediction(newpred);
                newpred.PredictionWindowSeconds++;
                API.Helix.Predictions.CreatePrediction(newpred);

                _time = TimeSpan.FromSeconds(newpred.PredictionWindowSeconds);

                _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
                {
                    Timer.Content = _time.ToString("c");
                    if (_time == TimeSpan.Zero) _timer.Stop();
                    _time = _time.Add(TimeSpan.FromSeconds(-1));
                }, Application.Current.Dispatcher);

            }

            catch (TwitchLib.Api.Core.Exceptions.BadRequestException)
            {
            }

        }
        DispatcherTimer _timer;
        TimeSpan _time;
        private void CancelPrediction(object sender, RoutedEventArgs e)
        {
            var resp = API.Helix.Predictions.GetPredictions(user).Result;
            if (resp.Data != null)
            {
                var latest = resp.Data[0];
                try
                {
                    API.Helix.Predictions.EndPrediction(user, latest.Id, TwitchLib.Api.Core.Enums.PredictionStatusEnum.LOCKED);
                    ClosePredButton.IsEnabled = false;
                    CancelPredButton.IsEnabled = false;
                    BlueWins.IsEnabled = false;
                    PinkWins.IsEnabled = false;
                    try
                    {
                        _timer.Stop();
                    }
                    catch { }
                    Timer.Content = "00:00:00";
                }

                catch (TwitchLib.Api.Core.Exceptions.BadRequestException) { }
            }
        }
    }
}
