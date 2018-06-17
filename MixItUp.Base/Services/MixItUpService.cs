﻿using Mixer.Base.Model.OAuth;
using Mixer.Base.Web;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.API;
using MixItUp.Base.Model.Store;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public interface IMixItUpService
    {
        Task RefreshOAuthToken();

        Task<MixItUpUpdateModel> GetLatestUpdate();

        Task SendLoginEvent(LoginEvent login);
        Task SendErrorEvent(ErrorEvent error);

        Task SendIssueReport(IssueReportModel report);

        Task<StoreDetailListingModel> GetStoreListing(Guid ID);
        Task AddStoreListing(StoreDetailListingModel listing);
        Task UpdateStoreListing(StoreDetailListingModel listing);
        Task DeleteStoreListing(Guid ID);

        Task<IEnumerable<StoreListingModel>> GetTopStoreListingsForTag(string tag);
        Task<StoreListingModel> GetTopRandomStoreListings();

        Task<IEnumerable<StoreListingModel>> SearchStoreListings(string search);

        Task AddStoreReview(StoreListingReviewModel review);
        Task UpdateStoreReview(StoreListingReviewModel review);

        Task AddStoreListingDownload(StoreListingModel listing);
        Task AddStoreListingUses(StoreListingUsesModel uses);
        Task AddStoreListingReport(StoreListingReportModel report);
    }

    public class MixItUpService : IMixItUpService, IDisposable
    {
        public const string MixItUpAPIEndpoint = "https://mixitupapi.azurewebsites.net/api/";

        private OAuthTokenModel token = null;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public MixItUpService()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(this.BackgroundCommandUsesUpload, this.cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed            
        }

        public async Task RefreshOAuthToken()
        {
            if (ChannelSession.User != null)
            {
                this.token = await this.GetAsync<OAuthTokenModel>("authentication?userID=" + ChannelSession.User.id);
            }
        }

        public async Task<MixItUpUpdateModel> GetLatestUpdate() { return await this.GetAsync<MixItUpUpdateModel>("updates"); }

        public async Task SendLoginEvent(LoginEvent login) { await this.PostAsync("login", login); }
        public async Task SendErrorEvent(ErrorEvent error) { await this.PostAsync("error", error); }

        public async Task SendIssueReport(IssueReportModel report) { await this.PostAsync("issuereport", report); }

        public async Task<StoreDetailListingModel> GetStoreListing(Guid ID)
        {
            StoreDetailListingModel listing = await this.GetAsync<StoreDetailListingModel>("store/details?id=" + ID);
            if (listing != null)
            {
                await listing.SetUser();
                await listing.SetReviewUsers();
            }
            return listing;
        }
        public async Task AddStoreListing(StoreDetailListingModel listing) { await this.PostAsync("store/details", listing); }
        public async Task UpdateStoreListing(StoreDetailListingModel listing) { await this.PutAsync("store/details", listing); }
        public async Task DeleteStoreListing(Guid ID) { await this.DeleteAsync("store/details?id=" + ID); }

        public async Task<IEnumerable<StoreListingModel>> GetTopStoreListingsForTag(string tag)
        {
            IEnumerable<StoreListingModel> listings = await this.GetAsync<IEnumerable<StoreListingModel>>("store/top?tag=" + tag);
            await this.SetStoreListingUsers(listings);
            return listings;
        }
        public async Task<StoreListingModel> GetTopRandomStoreListings()
        {
            IEnumerable<StoreListingModel> listings = await this.GetAsync<IEnumerable<StoreListingModel>>("store/top");
            if (listings != null && listings.Count() > 0)
            {
                StoreListingModel listing = listings.FirstOrDefault();
                await listing.SetUser();
                return listing;
            }
            return null;
        }

        public async Task<IEnumerable<StoreListingModel>> SearchStoreListings(string search)
        {
            IEnumerable<StoreListingModel> listings = await this.PostAsyncWithResult<IEnumerable<StoreListingModel>>("store/search?search=", search);
            await this.SetStoreListingUsers(listings);
            return listings;
        }

        public async Task AddStoreReview(StoreListingReviewModel review) { await this.PostAsync("store/reviews", review); }
        public async Task UpdateStoreReview(StoreListingReviewModel review) { await this.PutAsync("store/reviews", review); }

        public async Task AddStoreListingDownload(StoreListingModel listing) { await this.PostAsync("store/metadata/downloads", listing.ID); }
        public async Task AddStoreListingUses(StoreListingUsesModel uses) { await this.PostAsync("store/metadata/uses", uses); }
        public async Task AddStoreListingReport(StoreListingReportModel report) { await this.PostAsync("store/metadata/reports", report); }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    HttpResponseMessage response = await client.GetAsync(endpoint);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        return SerializerHelper.DeserializeFromString<T>(content);
                    }
                }
            }
            catch (Exception) { }
            return default(T);
        }

        private async Task PostAsync(string endpoint, object data)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    string content = SerializerHelper.SerializeToString(data);
                    HttpResponseMessage response = await client.PostAsync(endpoint, new StringContent(content, Encoding.UTF8, "application/json"));
                }
            }
            catch (Exception) { }
        }

        private async Task<T> PostAsyncWithResult<T>(string endpoint, object data)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    string content = SerializerHelper.SerializeToString(data);
                    HttpResponseMessage response = await client.PostAsync(endpoint, new StringContent(content, Encoding.UTF8, "application/json"));
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string resultContent = await response.Content.ReadAsStringAsync();
                        return SerializerHelper.DeserializeFromString<T>(resultContent);
                    }
                }
            }
            catch (Exception) { }
            return default(T);
        }

        private async Task PutAsync(string endpoint, object data)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    string content = SerializerHelper.SerializeToString(data);
                    HttpResponseMessage response = await client.PutAsync(endpoint, new StringContent(content, Encoding.UTF8, "application/json"));
                }
            }
            catch (Exception) { }
        }

        private async Task PatchAsync(string endpoint, object data)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    string content = SerializerHelper.SerializeToString(data);
                    HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint);
                    message.Content = new StringContent(content, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.SendAsync(message);
                }
            }
            catch (Exception) { }
        }

        private async Task DeleteAsync(string endpoint)
        {
            try
            {
                using (HttpClientWrapper client = new HttpClientWrapper(MixItUpAPIEndpoint))
                {
                    HttpResponseMessage response = await client.DeleteAsync(endpoint);
                }
            }
            catch (Exception) { }
        }

        private async Task SetStoreListingUsers(IEnumerable<StoreListingModel> listings)
        {
            if (listings != null)
            {
                foreach (StoreListingModel listing in listings)
                {
                    await listing.SetUser();
                }
            }
        }

        private async Task BackgroundCommandUsesUpload()
        {
            while (!this.cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Dictionary<Guid, long> commandUses = CommandBase.GetCommandUses();
                    foreach (var kvp in commandUses)
                    {
                        await this.AddStoreListingUses(new StoreListingUsesModel() { ID = kvp.Key, Uses = kvp.Value });
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex) { MixItUp.Base.Util.Logger.Log(ex); }

                await Task.Delay(60000);
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
