using System;
using System.Collections.Generic;
using System.Linq;

namespace JustClimbTrial.DataAccess
{
    public enum EntityType
    {
        AG,  // age group
        BR,  // boulder route
        BV,  // boulder route video
        RB,  // rock on boulder route
        RD,  // route difficulty
        RO,  // rock
        RT,  // rock on training route
        TR,  // training route
        TV,  // training route video
        WA   // wall
    }

    public class KeyGenerator
    {
        public static Dictionary<EntityType, string> EntityKeyPrefices = new Dictionary<EntityType, string>
        {
            { EntityType.AG, "AG" },
            { EntityType.BR, "BR" },
            { EntityType.BV, "BV" },
            { EntityType.RB, "RB" },
            { EntityType.RD, "RD" },
            { EntityType.RO, "RO" },
            { EntityType.RT, "RT" },
            { EntityType.TR, "TR" },
            { EntityType.TV, "TV" },
            { EntityType.WA, "WA" }
        };

        public static Dictionary<EntityType, string> EntityNoPrefices = new Dictionary<EntityType, string>
        {
            { EntityType.AG, "AG" },
            { EntityType.BR, "BR" },
            { EntityType.BV, "BV" },
            { EntityType.RB, "RB" },
            { EntityType.RD, "RD" },
            { EntityType.RO, "RO" },
            { EntityType.RT, "RT" },
            { EntityType.TR, "TR" },
            { EntityType.TV, "TV" },
            { EntityType.WA, "WA" }
        };

        private const int RequiredKeyLength = 20;
        private const int RequiredNoLength = 20;

        private static Random RandObj = new Random();
        private const string CharCollection = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // !!! Important !!!
        // if using long time string (18 characters) + 2-character prefix, no random characters can be appended
        // this may result in duplicated key
        // so use short time string (12 characters)
        private const string LongTimeStampFormat = "{0:yyyyMMddHHmmssffff}";
        private const string ShortTimeStampFormat = "{0:yyyyMMddHHmm}";


        /* public methods */

        public static string GenerateNewKey(EntityType type, DateTime createDT)
        {
            string newKey = EntityKeyPrefices[type] + DateTimeString(createDT);
            int lengthOfRandomSuffix = RequiredKeyLength - newKey.Length;
            return (lengthOfRandomSuffix > 0) ? newKey + RandomString(lengthOfRandomSuffix) : newKey;
        }

        public static string GenerateNewKey(EntityType type, out DateTime createDT)
        {
            createDT = DateTime.Now;
            return GenerateNewKey(type, createDT);
        }

        public static string GenerateNewKey(EntityType type)
        {
            DateTime tmpDT;
            return GenerateNewKey(type, out tmpDT);
        }

        public static string GenerateNewNo(EntityType type, DateTime createDT)
        {
            string newNo = EntityNoPrefices[type] + DateTimeString(createDT);
            int lengthOfRandomSuffix = RequiredNoLength - newNo.Length;
            return (lengthOfRandomSuffix > 0) ? newNo + RandomString(lengthOfRandomSuffix) : newNo;
        }

        public static string GenerateNewNo(EntityType type, out DateTime createDT)
        {
            createDT = DateTime.Now;
            return GenerateNewNo(type, createDT);
        }

        public static string GenerateNewNo(EntityType type)
        {
            DateTime tmpDT;
            return GenerateNewNo(type, out tmpDT);
        }        

        public static Tuple<string, string> GenerateNewKeyAndNo(EntityType type, DateTime createDT)
        {
            string newKey = GenerateNewKey(type, createDT);
            string newNo = GenerateNewNo(type, createDT);
            return new Tuple<string, string>(newKey, newNo);
        }

        public static Tuple<string, string> GenerateNewKeyAndNo(EntityType type, out DateTime createDT)
        {
            createDT = DateTime.Now;
            return GenerateNewKeyAndNo(type, createDT);
        }

        public static Tuple<string, string> GenerateNewKeyAndNo(EntityType type)
        {
            DateTime tmpDT;
            return GenerateNewKeyAndNo(type, out tmpDT);
        }

        /* end of public methods */


        /* private methods */

        // http://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
        private static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(CharCollection, length)
                .Select(s => s[RandObj.Next(s.Length)]).ToArray());
        }        

        private static string DateTimeString(DateTime aDT)
        {
            return String.Format(ShortTimeStampFormat, aDT);
        }

        private static string DateTimeString(out DateTime aDT)
        {
            aDT = DateTime.Now;
            return DateTimeString(aDT);
        }

        private static string DateTimeString()
        {
            DateTime tmpDT;
            return DateTimeString(out tmpDT);
        }

        /* end of private methods */
    }
}
