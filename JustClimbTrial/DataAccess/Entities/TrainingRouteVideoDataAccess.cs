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

        public static string InsertToReplacePreviousDemo(TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            SetIsDemoOfExistingDemoRouteVideoToFalse(proposedVideo.Route, isSubmitChanges);
            proposedVideo.IsDemo = true;  // TODO: should force set IsDemo here?
            return Insert(proposedVideo, isSubmitChanges);
        }

        public static TrainingRouteVideo Insert(string routeId, bool isDemo, bool isSubmitChanges = true)
        {
            TrainingRouteVideo trainingRouteVideo = new TrainingRouteVideo()
            {
                Route = routeId,
                IsDemo = isDemo
            };
            Insert(trainingRouteVideo, isSubmitChanges);
            return trainingRouteVideo;
        }

        public static string Insert(TrainingRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedVideo.IsDeleted = false;
            proposedVideo.CreateDT = createDT;

            Tuple<string, string> videoIdAndNo = KeyGenerator.GenerateNewKeyAndNo(myEntityType, createDT);
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
