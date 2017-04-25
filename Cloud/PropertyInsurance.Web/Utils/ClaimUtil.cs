﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyInsurance.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace PropertyInsurance.Web.Utils
{
    public class ClaimUtil
    {

        public static async Task<string> GetMockImageFromBlob()
        {
            var currentMtc = await GetCurrentMTC();
            var mtcName = currentMtc==null?string.Empty : GetPureJsonValue("name", currentMtc);
            var claimBlobImgName= string.Format("{0}_{1}",mtcName,Constants.blobImageName);
            var claimImage = BlobUtil.GetBlobSasUri(Constants.pictureBlobcontainer, claimBlobImgName);
            return claimImage;
        }

        public static async Task<string> GetMockCutomerEmail()
        {
            var currentMtc = await GetCurrentMTC();
            if (currentMtc == null)
                return string.Empty;
            var customer = currentMtc["customer"];
            var customerEmail = GetPureJsonValue("email", customer);
            return string.Format("{0}:{1}", "uid", customerEmail);
        }

        public static async Task<string> GetThumbnailString()
        {
            var graphClient = GetGraphClient();
            var user = await graphClient.GetCurrentUserAsync();
            return await GetThumbnail(graphClient, user.UserPrincipalName);
        }

        public static async Task<UserRole> GetMockUserRole()
        {
            var graphClient = GetGraphClient();
            var user = await graphClient.GetCurrentUserAsync();
            var dataJsonUrl = BlobUtil.GetBlobSasUri("public", "mtcs.json");
            var mtcName = string.Empty;
            var userEmail = user.Mail ?? string.Empty;
            UserRole result = UserRole.Manager;
            using (var httpClient = new HttpClient())
            {
                var jsonStr = await httpClient.GetStringAsync(dataJsonUrl);
                var mtcJson = JsonConvert.DeserializeObject(jsonStr) as JObject;
                foreach (var mtc in mtcJson["mtcs"])
                {
                    if (GetPureJsonValue("email", mtc["manager"]).ToLower() == userEmail.ToLower())
                    {
                        result = UserRole.Manager;
                        break;
                    }
                    if (GetPureJsonValue("email", mtc["adjuster"]).ToLower() == userEmail.ToLower())
                    {
                        result = UserRole.Adjuster;
                        break;
                    }
                    if (GetPureJsonValue("email", mtc["customer"]).ToLower() == userEmail.ToLower())
                    {
                        result = UserRole.Customer;
                        break;
                    }
                }
            }
            return result;
        }

        public static ClaimViewModel GetMockClaimViewModel()
        {
            return new ClaimViewModel()
            {
                Id = 1201707,
                RatePercent = 95,
                Status = "Pending",
                Date = DateTime.Now.ToString("MM/dd/yyyy hh:mmtt"),
                Description = "Our city was hit by an intense rainstorm on Sunday afternoon and our drainage pipe backed up. As a result, over a foot of water flooded our home, causing extensive damage to the floors as well as some furniture.",
                PolicyHolder = new PolicyHolder()
                {
                    Id = "134-000-1276",
                    Address = "700 120th St, Orofino, ID 83544",
                    RegisterDate = "March 31, 2002"
                }
            };
        }
        public static List<ClaimViewModel> GetMockClaimViewModelList()
        {
            return new List<ClaimViewModel>()
            {
                new ClaimViewModel()
                {
                    Id = 1201707,
                    RatePercent = 95,
                    Payout = "6,705.82",
                    Status = "Adjuster desk",
                    Date = DateTime.Now.ToString("M/d/yyyy h:mm tt")
                },
                new ClaimViewModel()
                {
                    Id = 1141698,
                    RatePercent = 80,
                    Payout = "11,223.70",
                    Status = "Completed",
                    Date = "4/3/2007 6:23 PM",
                },
                new ClaimViewModel()
                {
                    Id = 1021710,
                    RatePercent = 80,
                    Payout = "1,354.22",
                    Status = "Completed",
                    Date = "6/19/2004 5:14 PM",
                }
            };
        }

        public static List<BuilderViewModel> GetMockBuilderViewModelList()
        {
            var result = new List<BuilderViewModel>()
            {
                new BuilderViewModel()
                {
                    Name="Fabrikam Inc.",
                    TotalNoProperties=13,
                    TotalClaims=45,
                    PercentClaims=58,
                    Risk = Risk.Medium
                },
                new BuilderViewModel()
                {
                    Name="VanArsdel, Ltd.",
                    TotalNoProperties=24,
                    TotalClaims=9,
                    PercentClaims=12,
                    Risk = Risk.Low
                },
                new BuilderViewModel()
                {
                    Name="Fabrikam Residences",
                    TotalNoProperties=23,
                    TotalClaims=19,
                    PercentClaims=25,
                    Risk = Risk.Medium
                },
                new BuilderViewModel()
                {
                    Name="Proseware, Inc.",
                    TotalNoProperties=40,
                    TotalClaims=0,
                    PercentClaims=0,
                    Risk = Risk.Low
                },
                new BuilderViewModel()
                {
                    Name="Adatum Corporation",
                    TotalNoProperties=40,
                    TotalClaims=4,
                    PercentClaims=5,
                    Risk = Risk.Low
                }
            };
            return result.OrderBy(a => a.Name).ToList();
        }

        public static string GetBuilderHighRiskTrCssClass(BuilderViewModel model)
        {
            if (model.Risk == Risk.High)
                return Constants.BuilderHighRiskTrClass;
            else
                return string.Empty;
        }
        public static string GetBuilderHighRiskTdCssClass(BuilderViewModel model)
        {
            if (model.Risk == Risk.High)
                return Constants.BuilderHighRiskTdClass;
            else
                return string.Empty;
        }

        private static AADGraphClient GetGraphClient()
        {
            var activeDirectoryClient = AuthenticationHelper.GetActiveDirectoryClient();
            var graphClient = new AADGraphClient(activeDirectoryClient);
            return graphClient;
        }

        private static async Task<string> GetThumbnail(AADGraphClient client, string userPrincipalName)
        {
            var result = string.Empty;
            try
            {

                var userPhoto = await client.GetThumbnailAsync(userPrincipalName);
                if (userPhoto != null)
                {
                    result = userPhoto;
                }
            }
            catch (Microsoft.Data.OData.ODataErrorException ex)
            {
                if (ex.Error.ErrorCode == "Request_ResourceNotFound")
                {
                    result = Constants.AnonymPhotoPath;
                }
            }
            catch (Exception e)
            {
                result = Constants.AnonymPhotoPath;
            }
            return result;
        }

        private static string GetPureJsonValue(string jObjectKey,JToken obj)
        {
            var jsonValue = JsonConvert.SerializeObject(obj[jObjectKey]);
            return jsonValue.Substring(1, jsonValue.Length - 2);
        }

        private static async Task<JToken> GetCurrentMTC()
        {
            var graphClient = GetGraphClient();
            var user = await graphClient.GetCurrentUserAsync();
            var dataJsonUrl = BlobUtil.GetBlobSasUri("public", "mtcs.json");
            var mtcName = string.Empty;
            var userEmail = user.Mail ?? string.Empty;
            JToken result = null;
            using (var httpClient = new HttpClient())
            {
                var jsonStr = await httpClient.GetStringAsync(dataJsonUrl);
                var mtcJson = JsonConvert.DeserializeObject(jsonStr) as JObject;
                foreach (var mtc in mtcJson["mtcs"])
                {
                    if (GetPureJsonValue("email", mtc["manager"]).ToLower() == userEmail.ToLower())
                    {
                        result = mtc;
                        break;
                    }
                    if (GetPureJsonValue("email", mtc["adjuster"]).ToLower() == userEmail.ToLower())
                    {
                        result = mtc;
                        break;
                    }
                }
            }
            return result;
        }

    }
}