using UnityEngine;
using VoxelBusters.CoreLibrary;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System.Collections.Generic;

namespace VoxelBusters.AdsKit.Adapters
{
    public static class AdMobUtility
    {
        #region Static methods

        public static GoogleMobileAds.Api.AdSize ConvertToNativeAdSize(AdSize size)
        {
            int widthToRequest = size.Width <= 0 ? GoogleMobileAds.Api.AdSize.FullWidth : size.Width;

            GoogleMobileAds.Api.AdSize requiredAdSize;

            switch (size.Type)
            {
                case AdSize.AdSizeType.AnchoredAdaptive:
                    if (size.AnchorOrientation == ScreenOrientation.AutoRotation)
                    {
                        requiredAdSize = GoogleMobileAds.Api.AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(widthToRequest);
                    }
                    else if ((size.AnchorOrientation == ScreenOrientation.Portrait) || (size.AnchorOrientation == ScreenOrientation.PortraitUpsideDown))
                    {
                        requiredAdSize = GoogleMobileAds.Api.AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(widthToRequest);
                    }
                    else if ((size.AnchorOrientation == ScreenOrientation.LandscapeLeft) || (size.AnchorOrientation == ScreenOrientation.LandscapeRight))
                    {
                        requiredAdSize = GoogleMobileAds.Api.AdSize.GetLandscapeAnchoredAdaptiveBannerAdSizeWithWidth(widthToRequest);
                    }
                    else
                    {
                        throw VBException.NotImplemented();
                    }

                    break;

                case AdSize.AdSizeType.Fixed:
                default:
                    requiredAdSize = new GoogleMobileAds.Api.AdSize(size.Width, size.Height);
                    break;

            }

            Debug.Log("requiredAdSize : " + requiredAdSize.Width + " " + requiredAdSize.Height);

            return requiredAdSize;
        }

        public static GoogleMobileAds.Api.AdPosition ConvertToNativeAdPosition(AdPositionPreset value)
        {
            switch (value)
            {
                case AdPositionPreset.BottomCenter:
                    return GoogleMobileAds.Api.AdPosition.Bottom;

                case AdPositionPreset.BottomLeft:
                    return GoogleMobileAds.Api.AdPosition.BottomLeft;

                case AdPositionPreset.BottomRight:
                    return GoogleMobileAds.Api.AdPosition.BottomRight;

                case AdPositionPreset.Center:
                    return GoogleMobileAds.Api.AdPosition.Center;

                case AdPositionPreset.TopCenter:
                    return GoogleMobileAds.Api.AdPosition.Top;

                case AdPositionPreset.TopLeft:
                    return GoogleMobileAds.Api.AdPosition.TopLeft;

                case AdPositionPreset.TopRight:
                    return GoogleMobileAds.Api.AdPosition.TopRight;

                default:
                    return GoogleMobileAds.Api.AdPosition.Center;
            }
        }

        public static AdTransaction.PrecisionType ConvertToNativeAdTransactionPrecisionType(AdValue.PrecisionType precision)
        {
            switch (precision)
            {
                case AdValue.PrecisionType.Estimated:
                    return AdTransaction.PrecisionType.Estimated;

                case AdValue.PrecisionType.Precise:
                    return AdTransaction.PrecisionType.Precise;

                case AdValue.PrecisionType.PublisherProvided:
                    return AdTransaction.PrecisionType.PublisherProvided;

                default:
                    return AdTransaction.PrecisionType.Unknown;
            }
        }

        public static MaxAdContentRating ConvertToNativeContentRating(ContentRating? applicationContentRating)
        {
            if (applicationContentRating == null) return MaxAdContentRating.Unspecified;

            switch (applicationContentRating.Value)
            {
                case ContentRating.GeneralAudience:
                    return MaxAdContentRating.G;

                case ContentRating.MatureAudience:
                    return MaxAdContentRating.MA;
                    
                case ContentRating.ParentalGuidance:
                    return MaxAdContentRating.PG;

                case ContentRating.TeensAndOlder:
                    return MaxAdContentRating.T;

                case ContentRating.Unspecified:
                default:
                    return MaxAdContentRating.Unspecified;
            }
        }

        public static TagForUnderAgeOfConsent ConvertToNativeUnderAgeOfConsent(bool? ageRestrictedUser)
        {
            if (ageRestrictedUser == null) return TagForUnderAgeOfConsent.Unspecified;

            return ageRestrictedUser.Value ? TagForUnderAgeOfConsent.True : TagForUnderAgeOfConsent.False;
        }

        public static CoreLibrary.ConsentStatus ConvertToUnityConsentStatus(GoogleMobileAds.Ump.Api.ConsentStatus status)
        {
            switch (status)
            {
                case GoogleMobileAds.Ump.Api.ConsentStatus.NotRequired:
                case GoogleMobileAds.Ump.Api.ConsentStatus.Obtained:
                    return CoreLibrary.ConsentStatus.Authorized;

                case GoogleMobileAds.Ump.Api.ConsentStatus.Required:
                case GoogleMobileAds.Ump.Api.ConsentStatus.Unknown:
                default: 
                    return CoreLibrary.ConsentStatus.NotDetermined;
            }
        }

        public static ApplicationPrivacyConfiguration ConvertToPrivacyConfiguration(ConsentInformation information)
        {
            return new ApplicationPrivacyConfiguration(usageConsent: ConvertToUnityConsentStatus(ConsentInformation.ConsentStatus));
        }

        #endregion
    }
}
