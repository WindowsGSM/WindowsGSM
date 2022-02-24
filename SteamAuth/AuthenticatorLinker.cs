using System;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace SteamAuth
{
    /// <summary>
    /// Handles the linking process for a new mobile authenticator.
    /// </summary>
    public class AuthenticatorLinker
    {
        /// <summary>
        /// Set to register a new phone number when linking. If a phone number is not set on the account, this must be set. If a phone number is set on the account, this must be null.
        /// </summary>
        public string? PhoneNumber = null;

        /// <summary>
        /// Randomly-generated device ID. Should only be generated once per linker.
        /// </summary>
        public string DeviceID { get; private set; }

        /// <summary>
        /// After the initial link step, if successful, this will be the SteamGuard data for the account. PLEASE save this somewhere after generating it; it's vital data.
        /// </summary>
        public SteamGuardAccount? LinkedAccount { get; private set; }

        /// <summary>
        /// True if the authenticator has been fully finalized.
        /// </summary>
        public bool Finalized = false;

        private SessionData _session;
        private CookieContainer _cookies;
        private bool confirmationEmailSent = false;

        public AuthenticatorLinker(SessionData session)
        {
            this._session = session;
            this.DeviceID = GenerateDeviceID();

            this._cookies = new CookieContainer();
            session.AddCookies(_cookies);
        }

        public LinkResult AddAuthenticator()
        {
            bool hasPhone = _hasPhoneAttached();
            if (hasPhone && PhoneNumber != null)
                return LinkResult.MustRemovePhoneNumber;
            if (!hasPhone && PhoneNumber == null)
                return LinkResult.MustProvidePhoneNumber;

            if (!hasPhone) {
                if (confirmationEmailSent) {
                    if (!_checkEmailConfirmation()) {
                        return LinkResult.GeneralFailure;
                    }
                } else if (!_addPhoneNumber()) {
                    return LinkResult.GeneralFailure;
                } else {
                    confirmationEmailSent = true;
                    return LinkResult.MustConfirmEmail;
                }
            }

            var postData = new NameValueCollection();
            postData.Add("access_token", _session.OAuthToken);
            postData.Add("steamid", _session.SteamID.ToString());
            postData.Add("authenticator_type", "1");
            postData.Add("device_identifier", this.DeviceID);
            postData.Add("sms_phone_id", "1");

            string? response = SteamWeb.MobileLoginRequest(APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", postData);
            if (response == null) return LinkResult.GeneralFailure;

            var addAuthenticatorResponse = JsonSerializer.Deserialize<AddAuthenticatorResponse>(response);
            if (addAuthenticatorResponse == null || addAuthenticatorResponse.Response == null)
            {
                return LinkResult.GeneralFailure;
            }

            if (addAuthenticatorResponse.Response.Status == 29)
            {
                return LinkResult.AuthenticatorPresent;
            }

            if (addAuthenticatorResponse.Response.Status != 1)
            {
                return LinkResult.GeneralFailure;
            }

            this.LinkedAccount = addAuthenticatorResponse.Response;
            LinkedAccount.Session = this._session;
            LinkedAccount.DeviceID = this.DeviceID;

            return LinkResult.AwaitingFinalization;
        }

        public FinalizeResult FinalizeAddAuthenticator(string smsCode)
        {
            //The act of checking the SMS code is necessary for Steam to finalize adding the phone number to the account.
            //Of course, we only want to check it if we're adding a phone number in the first place...

            if (!String.IsNullOrEmpty(this.PhoneNumber) && !this._checkSMSCode(smsCode))
            {
                return FinalizeResult.BadSMSCode;
            }

            var postData = new NameValueCollection();
            postData.Add("steamid", _session.SteamID.ToString());
            postData.Add("access_token", _session.OAuthToken);
            postData.Add("activation_code", smsCode);
            int tries = 0;
            while (tries <= 30)
            {
                postData.Set("authenticator_code", LinkedAccount?.GenerateSteamGuardCode());
                postData.Set("authenticator_time", TimeAligner.GetSteamTime().ToString());

                string? response = SteamWeb.MobileLoginRequest(APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/FinalizeAddAuthenticator/v0001", "POST", postData);
                if (response == null) return FinalizeResult.GeneralFailure;

                var finalizeResponse = JsonSerializer.Deserialize<FinalizeAuthenticatorResponse>(response);

                if (finalizeResponse == null || finalizeResponse.Response == null)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeResponse.Response.Status == 89)
                {
                    return FinalizeResult.BadSMSCode;
                }

                if (finalizeResponse.Response.Status == 88)
                {
                    if (tries >= 30)
                    {
                        return FinalizeResult.UnableToGenerateCorrectCodes;
                    }
                }

                if (!finalizeResponse.Response.Success)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeResponse.Response.WantMore)
                {
                    tries++;
                    continue;
                }

                if (this.LinkedAccount != null)
                {
                    this.LinkedAccount.FullyEnrolled = true;
                }

                return FinalizeResult.Success;
            }

            return FinalizeResult.GeneralFailure;
        }

        private bool _checkSMSCode(string smsCode)
        {
            var postData = new NameValueCollection();
            postData.Add("op", "check_sms_code");
            postData.Add("arg", smsCode);
            postData.Add("checkfortos", "0");
            postData.Add("skipvoip", "1");
            postData.Add("sessionid", _session.SessionID);

            string? response = SteamWeb.Request(APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, _cookies);
            if (response == null) return false;

            var addPhoneNumberResponse = JsonSerializer.Deserialize<AddPhoneResponse>(response)!;

            if (!addPhoneNumberResponse.Success)
            {
                Thread.Sleep(3500); //It seems that Steam needs a few seconds to finalize the phone number on the account.
                return _hasPhoneAttached();
            }

            return true;
        }

        private bool _addPhoneNumber()
        {
            var postData = new NameValueCollection();
            postData.Add("op", "add_phone_number");
            postData.Add("arg", PhoneNumber);
            postData.Add("sessionid", _session.SessionID);

            string? response = SteamWeb.Request(APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, _cookies);
            if (response == null) return false;

            var addPhoneNumberResponse = JsonSerializer.Deserialize<AddPhoneResponse>(response)!;
            return addPhoneNumberResponse.Success;
        }

        private bool _checkEmailConfirmation() {
            var postData = new NameValueCollection();
            postData.Add("op", "email_confirmation");
            postData.Add("arg", "");
            postData.Add("sessionid", _session.SessionID);

            string? response = SteamWeb.Request(APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, _cookies);
            if (response == null) return false;

            var emailConfirmationResponse = JsonSerializer.Deserialize<AddPhoneResponse>(response)!;
             return emailConfirmationResponse.Success;
        }

        private bool _hasPhoneAttached() {
            var postData = new NameValueCollection();
            postData.Add("op", "has_phone");
            postData.Add("arg", "null");
            postData.Add("sessionid", _session.SessionID);

            string? response = SteamWeb.Request(APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, _cookies);
            if (response == null) return false;

            var hasPhoneResponse = JsonSerializer.Deserialize<HasPhoneResponse>(response)!;
            return hasPhoneResponse.HasPhone;
        }

        public enum LinkResult
        {
            MustProvidePhoneNumber, //No phone number on the account
            MustRemovePhoneNumber, //A phone number is already on the account
            MustConfirmEmail, //User need to click link from confirmation email
            AwaitingFinalization, //Must provide an SMS code
            GeneralFailure, //General failure (really now!)
            AuthenticatorPresent
        }

        public enum FinalizeResult
        {
            BadSMSCode,
            UnableToGenerateCorrectCodes,
            Success,
            GeneralFailure
        }

        private class AddAuthenticatorResponse
        {
            [JsonPropertyName("response")]
            public SteamGuardAccount? Response { get; set; }
        }

        private class FinalizeAuthenticatorResponse
        {
            [JsonPropertyName("response")]
            public FinalizeAuthenticatorInternalResponse? Response { get; set; }

            internal class FinalizeAuthenticatorInternalResponse
            {
                [JsonPropertyName("status")]
                public int Status { get; set; }

                [JsonPropertyName("server_time")]
                public long ServerTime { get; set; }

                [JsonPropertyName("want_more")]
                public bool WantMore { get; set; }

                [JsonPropertyName("success")]
                public bool Success { get; set; }
            }
        }

        private class HasPhoneResponse
        {
            [JsonPropertyName("has_phone")]
            public bool HasPhone { get; set; }
        }

        private class AddPhoneResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }

        public static string GenerateDeviceID()
        {
          return "android:" + Guid.NewGuid().ToString();
        }
    }
}
