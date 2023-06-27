using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TmCGPTD
{ 
    class CustomSessionHandler : IGotrueSessionPersistence<Session>
    {
        public void SaveSession(Session session)
        {
            // Persist Session in Filesystem or in browser storage
            // JsonConvert.SerializeObject(session) will be helpful here!
            if (session != null)
            {
                AppSettings.Instance.Session = JsonSerializer.Serialize(session);
            }
            else
            {
                return;
            }
        }

        public void DestroySession()
        {
            // Destroy Session on Filesystem or in browser storage
            //throw new NotImplementedException();
        }

        public Session? LoadSession()
        {
            // Retrieve Session from Filesystem or from browser storage
            // JsonConvert.DeserializeObject<TSession>(value) will be helpful here!
            if (AppSettings.Instance.Session != null)
            {
                return JsonSerializer.Deserialize<Session>(AppSettings.Instance.Session)!;
            }
            else
            {
                return null;
            }
        }
    }
}
