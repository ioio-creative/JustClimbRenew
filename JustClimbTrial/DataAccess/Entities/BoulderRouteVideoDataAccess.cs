using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class BoulderRouteVideoDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.BV;

        public static IEnumerable<BoulderRouteVideo> BoulderRouteVideos
        {
            get
            {
                return database.BoulderRouteVideos;
            }
        }

        public static IEnumerable<BoulderRouteVideo> ValidBoulderRouteVideos
        {
            get
            {
                return BoulderRouteVideos.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static IEnumerable<BoulderRouteVideo> BoulderRouteVideosByRouteId(string routeId)
        {
            return BoulderRouteVideos.Where(x => x.Route == routeId);
        }

        public static IEnumerable<BoulderRouteVideo> ValidBoulderRouteVideosByRouteId(string routeId)
        {
            return ValidBoulderRouteVideos.Where(x => x.Route == routeId);
        }

        public static BoulderRouteVideo ValidBoulderRouteDemoVideoByRouteId(string routeId)
        {
            return ValidBoulderRouteVideosByRouteId(routeId).Single(x => x.IsDemo.GetValueOrDefault(false));
        }

        public static BoulderRouteVideo TryGetValidBoulderRouteDemoVideoByRouteId(string routeId)
        {
            return ValidBoulderRouteVideosByRouteId(routeId).SingleOrDefault(x => x.IsDemo.GetValueOrDefault(false));
        }

        public static BoulderRouteVideo BoulderRouteVideoById(string videoId)
        {
            return BoulderRouteVideos.Single(x => x.VideoID == videoId);
        }

        public static BoulderRouteVideo InsertToReplacePreviousDemo(string routeId, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(routeId, isSubmitChanges);
            return Insert(routeId, true, isSubmitChanges);
        }

        public static BoulderRouteVideo InsertToReplacePreviousDemo(Tuple<string, string> videoIdAndNo, string routeId, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(routeId, isSubmitChanges);
            return Insert(videoIdAndNo, routeId, true, isSubmitChanges);
        }

        public static string InsertToReplacePreviousDemo(BoulderRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(proposedVideo.Route, isSubmitChanges);
            proposedVideo.IsDemo = true;  // TODO: should force set IsDemo here?
            return Insert(proposedVideo, isSubmitChanges);
        }

        public static string InsertToReplacePreviousDemo(Tuple<string, string> videoIdAndNo, BoulderRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(proposedVideo.Route, isSubmitChanges);
            proposedVideo.IsDemo = true;  // TODO: should force set IsDemo here?
            return Insert(videoIdAndNo, proposedVideo, isSubmitChanges);
        }

        public static BoulderRouteVideo Insert(string routeId, bool isDemo, bool isSubmitChanges = true)
        {
            BoulderRouteVideo routeVideo = new BoulderRouteVideo()
            {
                Route = routeId,
                IsDemo = isDemo            
            };
            Insert(routeVideo, isSubmitChanges);
            return routeVideo;
        }

        public static BoulderRouteVideo Insert(Tuple<string, string> videoIdAndNo, string routeId, bool isDemo, bool isSubmitChanges = true)
        {
            BoulderRouteVideo routeVideo = new BoulderRouteVideo()
            {
                Route = routeId,
                IsDemo = isDemo
            };
            Insert(videoIdAndNo, routeVideo, isSubmitChanges);
            return routeVideo;
        }

        public static string Insert(BoulderRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            return Insert(GenerateIdAndNo(), proposedVideo, isSubmitChanges);
        }

        public static string Insert(Tuple<string, string> videoIdAndNo, BoulderRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            proposedVideo.IsDeleted = false;
            proposedVideo.CreateDT = DateTime.Now;

            proposedVideo.VideoID = videoIdAndNo.Item1;
            proposedVideo.VideoNo = videoIdAndNo.Item2;

            database.BoulderRouteVideos.InsertOnSubmit(proposedVideo);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedVideo.VideoID;
        }

        static void SetIsDeletedToTrue(string videoId, bool isSubmitChanges = true)
        {
            BoulderRouteVideo videoToDelete = BoulderRouteVideoById(videoId);
            videoToDelete.IsDeleted = true;
            videoToDelete.DeletedDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        //Generate VideoId
        public static Tuple<string, string> GenerateIdAndNo()
        {
            return KeyGenerator.GenerateNewKeyAndNo(myEntityType, DateTime.Now);
        }


        #region private methods

        private static void SetIsDemoOfExistingDemoRouteVideoToFalse(string routeId, bool isSubmitChanges = true)
        {
            BoulderRouteVideo existingDemoVideo = ValidBoulderRouteDemoVideoByRouteId(routeId);
            existingDemoVideo.IsDemo = false;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }

        #endregion
    }
}
