using Supabase.Gotrue;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TmCGPTD
{
    public class SupabaseStates : ObservableObject
    {
        private static SupabaseStates? _instance;
        public static SupabaseStates Instance
        {
            get
            {
                _instance ??= new SupabaseStates();

                return _instance;
            }
        }

        public AesSettings? AesSettings { get; set; }
        public Supabase.Client? Supabase { get; set; }
        public ProviderAuthState? AuthState { get; set; }
        public Supabase.Realtime.Client? RealtimeClient { get; set; }
    }
}
