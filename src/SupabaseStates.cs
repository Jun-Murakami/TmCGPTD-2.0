using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TmCGPTD
{
    public class SupabaseStates
    {
        private static SupabaseStates? _instance;

        public static SupabaseStates Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SupabaseStates();
                }

                return _instance;
            }
        }

        public Supabase.Client? Supabase { get; set; }
        public ProviderAuthState? AuthState { get; set; }


    }
}
