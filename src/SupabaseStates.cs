﻿using Supabase.Gotrue;
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

    public Supabase.Client? Supabase { get; set; }
    public ProviderAuthState? AuthState { get; set; }
  }
}
