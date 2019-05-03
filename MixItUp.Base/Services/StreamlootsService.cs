﻿using Mixer.Base.Model.OAuth;
using Mixer.Base.Model.User;
using Mixer.Base.Util;
using MixItUp.Base.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public class StreamlootsCardModel
    {
        private const string messageStartRegex = "^Hey ";
        private const string messageMiddleRegex = ", greetings from ";
        private const string messageEndRegex = " !$";

        public string imageUrl { get; set; }
        public string message { get; set; }
        public string username { get; set; }

        public void InitializeData()
        {
            this.message = Regex.Replace(this.message, messageStartRegex, string.Empty);
            this.message = Regex.Replace(this.message, messageEndRegex, string.Empty);
            string[] splits = this.message.Split(new string[] { messageMiddleRegex }, StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length == 2)
            {
                this.message = splits[0];
                this.username = splits[1];
            }
        }
    }

    public interface IStreamlootsService
    {
        Task<bool> Connect();

        Task Disconnect();

        OAuthTokenModel GetOAuthTokenCopy();
    }

    public class StreamlootsService : IStreamlootsService, IDisposable
    {
        private OAuthTokenModel token;

        private WebRequest webRequest;
        private Stream responseStream;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public StreamlootsService(string streamlootsID) : this(new OAuthTokenModel() { accessToken = streamlootsID }) { }

        public StreamlootsService(OAuthTokenModel token) { this.token = token; }

        public Task<bool> Connect()
        {
            try
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(this.BackgroundCheck, this.cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return Task.FromResult(true);
            }
            catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }
            return Task.FromResult(false);
        }

        public Task Disconnect()
        {
            this.cancellationTokenSource.Cancel();
            this.token = null;
            if (this.webRequest != null)
            {
                this.webRequest.Abort();
                this.webRequest = null;
            }
            if (this.responseStream != null)
            {
                this.responseStream.Close();
                this.responseStream = null;
            }
            return Task.FromResult(0);
        }

        public OAuthTokenModel GetOAuthTokenCopy()
        {
            return this.token;
        }

        private async Task BackgroundCheck()
        {
            this.webRequest = WebRequest.Create(string.Format("https://widgets.streamloots.com/alerts/{0}/media-stream", this.token.accessToken));
            ((HttpWebRequest)this.webRequest).AllowReadStreamBuffering = false;
            var response = this.webRequest.GetResponse();
            this.responseStream = response.GetResponseStream();

            UTF8Encoding encoder = new UTF8Encoding();
            var buffer = new byte[100000];
            while (true)
            {
                try
                {
                    if (this.responseStream.CanRead)
                    {
                        int len = this.responseStream.Read(buffer, 0, 100000);
                        if (len > 10)
                        {
                            string text = encoder.GetString(buffer, 0, len);
                            if (!string.IsNullOrEmpty(text))
                            {
                                JObject jobj = JObject.Parse("{ " + text + " }");
                                if (jobj != null && jobj.ContainsKey("data"))
                                {
                                    StreamlootsCardModel card = jobj["data"].ToObject<StreamlootsCardModel>();
                                    if (card != null)
                                    {
                                        card.InitializeData();

                                        UserViewModel user = new UserViewModel(0, card.username);

                                        UserModel userModel = await ChannelSession.Connection.GetUser(user.UserName);
                                        if (userModel != null)
                                        {
                                            user = new UserViewModel(userModel);
                                        }

                                        EventCommand command = ChannelSession.Constellation.FindMatchingEventCommand(EnumHelper.GetEnumName(OtherEventTypeEnum.StreamlootsCardUsed));
                                        if (command != null)
                                        {
                                            Dictionary<string, string> specialIdentifiers = new Dictionary<string, string>();
                                            specialIdentifiers.Add("streamlootscardname", "TEST CARD");
                                            specialIdentifiers.Add("streamlootscardimage", card.imageUrl);
                                            specialIdentifiers.Add("streamlootsmessage", card.message);
                                            await command.Perform(user, arguments: null, extraSpecialIdentifiers: specialIdentifiers);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log(ex);
                }
                await Task.Delay(1000);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                    this.cancellationTokenSource.Dispose();
                    if (this.webRequest != null)
                    {
                        this.webRequest.Abort();
                        this.webRequest = null;
                    }
                    if (this.responseStream != null)
                    {
                        this.responseStream.Close();
                        this.responseStream = null;
                    }
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
