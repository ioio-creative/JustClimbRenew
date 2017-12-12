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

        public static BoulderRouteVideo BoulderRouteVideoById(string videoId)
        {
            return BoulderRouteVideos.Where(x => x.VideoID == videoId).Single();
        }

        public static string Insert(BoulderRouteVideo proposedVideo, bool isSubmitChanges = true)
        {
            DateTime createDT = DateTime.Now;
            proposedVideo.IsDeleted = false;
            proposedVideo.CreateDT = createDT;

            Tuple<string, string> videoIdAndNo = KeyGenerator.GenerateNewKeyAndNo(myEntityType, createDT);
            proposedVideo.VideoID = videoIdAndNo.Item1;
            proposedVideo.VideoNo = videoIdAndNo.Item2;

            database.BoulderRouteVideos.InsertOnSubmit(proposedVideo);

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }

            return proposedVideo.VideoID;
        }

        public static void SetIsDeletedToTrue(string videoId, bool isSubmitChanges = true)
        {
            BoulderRouteVideo videoToDelete = BoulderRouteVideoById(videoId);
            videoToDelete.IsDeleted = true;
            videoToDelete.DeletedDT = DateTime.Now;

            if (isSubmitChanges)
            {
                database.SubmitChanges();
            }
        }
    }
}
