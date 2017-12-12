using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess.Entities
{
    public class TrainingRouteVideoDataAccess : DataAccessBase
    {
        private static EntityType myEntityType = EntityType.TR;

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

        public static TrainingRouteVideo TrainingRouteVideoById(string videoId)
        {
            return TrainingRouteVideos.Where(x => x.VideoID == videoId).Single();
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
    }
}
