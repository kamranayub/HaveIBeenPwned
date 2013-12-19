using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Tasks;

namespace HaveIBeenPwned
{
    public class MainPageViewModel : Screen
    {
        private readonly IEventAggregator _events;
        private bool _isPwned;
        private bool _isSafe;
        private string _error;
        private string _email;
        private string _pwnedSites;
        private bool _isFetching;

        public MainPageViewModel(IEventAggregator events) {
            if (events == null) throw new ArgumentNullException("events");
            _events = events;
        }

        protected override void OnInitialize() {
            base.OnInitialize();

            if (CanCheck) {
                Check();
            }
        }

        public async void Check()
        {

            PwnedSites = null;
            Error = null;
            IsPwned = false;
            IsSafe = false;

            try
            {
                await CheckEmail();
            }
            catch (Exception)
            {
                if (Debugger.IsAttached)
                {
                    throw;
                }
                IsFetching = false;
                Error = "Sorry, we couldn't check if you were pwned.";
            }
        }

        private async Task CheckEmail() {

            IsFetching = true;

            var client = new HttpClient();

            var response = await client.GetAsync(new Uri("https://haveibeenpwned.com/api/breachedaccount/" + Email));

            IsFetching = false;

            if (response.IsSuccessStatusCode)
            {

                var responseBody = await response.Content.ReadAsStringAsync();

                var pwnedSites = fastJSON.JSON.Instance.ToObject<List<string>>(responseBody);

                if (pwnedSites.Count > 0)
                {
                    IsPwned = true;
                    PwnedSites = String.Join("\r\n", pwnedSites);
                }
                else
                {
                    IsSafe = true;
                }
            }
            else if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                Error = "Your email address doesn't appear to be valid";
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                IsSafe = true;
            }
            else
            {
                Error = "Sorry, we couldn't check if you were pwned. Status: " + response.ReasonPhrase;
            }
        }

        public void LearnMore() {
            _events.RequestTask<WebBrowserTask>(t => t.Uri = new Uri("http://haveibeenpwned.com/email/" + Email));
        }

        public void ViewPrivacyPolicy() {
            _events.RequestTask<WebBrowserTask>(t => t.Uri = new Uri("http://haveibeenpwned.com/FAQs"));
        }

        public void ViewMoreApps() {
            _events.RequestTask<MarketplaceSearchTask>(t =>
            {
                t.SearchTerms = "Kamran Ayub";
                t.ContentType = MarketplaceContentType.Applications;
            });
        }

        public void ViewAbout() {
            _events.RequestTask<WebBrowserTask>(t => t.Uri = new Uri("http://haveibeenpwned.com/"));
        }

        public bool CanCheck
        {
            get
            {
                return !String.IsNullOrWhiteSpace(Email);
            }
        }

        public bool IsFetching {
            get { return _isFetching; }
            set {
                if (value.Equals(_isFetching)) return;
                _isFetching = value;
                NotifyOfPropertyChange(() => IsFetching);
                NotifyOfPropertyChange(() => HaveResponse);
            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                if (value == _email) return;
                _email = value;
                NotifyOfPropertyChange(() => Email);
                NotifyOfPropertyChange(() => CanCheck);
            }
        }

        public bool IsPwned
        {
            get { return _isPwned; }
            set
            {
                if (value.Equals(_isPwned)) return;
                _isPwned = value;
                NotifyOfPropertyChange(() => IsPwned);
                NotifyOfPropertyChange(() => HaveResponse);
            }
        }

        public string PwnedSites
        {
            get { return _pwnedSites; }
            set
            {
                if (value == _pwnedSites) return;
                _pwnedSites = value;
                NotifyOfPropertyChange(() => PwnedSites);
            }
        }

        public bool IsSafe
        {
            get { return _isSafe; }
            set
            {
                if (value.Equals(_isSafe)) return;
                _isSafe = value;
                NotifyOfPropertyChange(() => IsSafe);
                NotifyOfPropertyChange(() => HaveResponse);
            }
        }

        public bool HaveResponse
        {
            get {
                if (IsFetching) return true;

                return IsSafe || IsPwned;
            }
        }

        public string Error
        {
            get { return _error; }
            set
            {
                if (value == _error) return;
                _error = value;
                NotifyOfPropertyChange(() => Error);
            }
        }
    }

    public class MainPageStorage : StorageHandler<MainPageViewModel> {
        public override void Configure() {
            Property(m => m.Email).InAppSettings();
        }
    }
}