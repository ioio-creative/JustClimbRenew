using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class TrainingRouteVideoDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.TV;

        public static IEnumerable<TrainingRouteVideo> TrainingRouteVideos
        {
            get
            {
                return database.TrainingRouteVideos;
            }
        }

        public static IEnumerable<TrainingRouteVideo> ValidTrainingRouteVideos
        {
            get
            {
                return TrainingRouteVideos.Where(x => x.IsDeleted.GetValueOrDefault(false) == false);
            }
        }

        public static IEnumerable<TrainingRouteVideo> TrainingRouteVideosByRouteId(string routeId)
        {
            return TrainingRouteVideos.Where(x => x.Route == routeId);
        }

        public static IEnumerable<TrainingRouteVideo> ValidTrainingRouteVideosByRouteId(string routeId)
        {
            return ValidTrainingRouteVideos.Where(x => x.Route == routeId);
        }

        public static TrainingRouteVideo ValidTrainingRouteDemoVideoByRouteId(string routeId)
        {
            return ValidTrainingRouteVideosByRouteId(routeId).Single(x => x.IsDemo.GetValueOrDefault(false));
        }

        public static TrainingRouteVideo TryGetValidTrainingRouteDemoVideoByRouteId(string routeId)
        {
            return ValidTrainingRouteVideosByRouteId(routeId).SingleOrDefault(x => x.IsDemo.GetValueOrDefault(false));
        }

        public static TrainingRouteVideo TrainingRouteVideoById(string videoId)
        {
            return TrainingRouteVideos.Single(x => x.VideoID == videoId);
        }

        public static TrainingRouteVideo InsertToReplacePreviousDemo(string routeId, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(routeId, isSubmitChanges);
            return Insert(routeId, true, isSubmitChanges);
        }

        public static TrainingRouteVideo InsertToReplacePreviousDemo(Tuple<string, string> videoIdAndNo, string routeId, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(routeId, isSubmitChanges);
            return Insert(videoIdAndNo, routeId, true, isSubmitChanges);
        }

        public static string InsertToReplacePreviousDemo(TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(proposedVideo.Route, isSubmitChanges);
            proposedVideo.IsDemo = true;  // TODO: should force set IsDemo here?
            return Insert(proposedVideo, isSubmitChanges);
        }

        public static string InsertToReplacePreviousDemo(Tuple<string, string> videoIdAndNo, TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(proposedVideo.Route, isSubmitChanges);
            proposedVideo.IsDemo = true;  // TODO: should force set IsDemo here?
            return Insert(videoIdAndNo, proposedVideo, isSubmitChanges);
        }

        public static TrainingRouteVideo Insert(string routeId, bool isDemo, bool isSubmitChanges = true)
        {
            TrainingRouteVideo routeVideo = new TrainingRouteVideo()
            {
                Route = routeId,
                IsDemo = isDemo
            };
            Insert(routeVideo, isSubmitChanges);
            return routeVideo;
        }

        public static TrainingRouteVideo Insert(Tuple<string, string> videoIdAndNo, string routeId, bool isDemo, bool isSubmitChanges = true)
        {
            TrainingRouteVideo routeVideo = new TrainingRouteVideo()
            {
                Route = routeId,
                IsDemo = isDemo
            };
            Insert(videoIdAndNo, routeVideo, isSubmitChanges);
            return routeVideo;
        }

        public static string Insert(TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {           
            return Insert(GenerateIdAndNo(), proposedVideo, isSubmitChanges);
        }

        public static string Insert(Tuple<string, string> videoIdAndNo, TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            proposedVideo.IsDeleted = false;
            proposedVideo.CreateDT = DateTime.Now;
            
            proposedVideo.VideoID = videoIdAndNo.Item1;
            proposedVideo.VideoNo = videoIdAndNo.Item2;

            database.TrainingRouteVideos.InsertOnSubmit(proposedVideo);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedVideo.VideoID;
        }

        public static void SetIsDeletedToTrue(string videoId, bool isSubmitChanges = true)
        {
            TrainingRouteVideo videoToDelete = TrainingRouteVideoById(videoId);
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
            TrainingRouteVideo existingDemoVideo = ValidTrainingRouteDemoVideoByRouteId(routeId);
            existingDemoVideo.IsDemo = false;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        } 
        
        #endregion
    }
}
