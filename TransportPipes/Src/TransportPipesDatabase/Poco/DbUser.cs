using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco
{
    public class DbUser
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Index(true)]
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public double Gold { get; set; }

        [Reference]
        public List<DbGameObject> GameObjects { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Id, UserName);
        }

        /// <summary>
        /// Required for Dictionary
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var objAsDbUser = obj as DbUser;

            if (objAsDbUser == null)
            {
                if (this == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return (objAsDbUser.Id == this.Id);
        }

        /// <summary>
        /// Required for Dictionary
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = 0;
                result = (result * 397) ^ Id.GetHashCode();
                return result;
            }
        }
    }
}
