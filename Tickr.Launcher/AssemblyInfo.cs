// ─────────────────────────────────────────────────────────────────────────────
//  Tickr.Launcher — AssemblyInfo.cs  (test visibility, signing-aware)
// ─────────────────────────────────────────────────────────────────────────────

using System.Runtime.CompilerServices;

// PublicKey must match resources/Tickr.snk.pub - the same key Tickr/SharedInfo.cs exposes
#if TICKR_SIGNED_BUILD
[assembly: InternalsVisibleTo($"Tickr.Tests, PublicKey={Tickr.Launcher.LauncherPublicKey.Value}")]
#else
[assembly: InternalsVisibleTo("Tickr.Tests")]
#endif

namespace Tickr.Launcher;

internal static class LauncherPublicKey {
	internal const string Value = "002400000480000014020000060200000024000052534131001000000100010099f0e5961ec7497fd7de1cba2b8c5eff3b18c1faf3d7a8d56e063359c7f928b54b14eae24d23d9d3c1a5db7ceca82edb6956d43e8ea2a0b7223e6e6836c0b809de43fde69bf33fba73cf669e71449284d477333d4b6e54fb69f7b6c4b4811b8fe26e88975e593cffc0e321490a50500865c01e50ab87c8a943b2a788af47dc20f2b860062b7b6df25477e471a744485a286b435cea2df3953cbb66febd8db73f3ccb4588886373141d200f749ba40bb11926b668cc15f328412dd0b0b835909229985336eb4a34f47925558dc6dc3910ea09c1aad5c744833f26ad9de727559d393526a7a29b3383de87802a034ead8ecc2d37340a5fa9b406774446256337d77e3c9e8486b5e732097e238312deaf5b4efcc04df8ecb986d90ee12b4a8a9a00319cc25cb91fd3e36a3cc39e501f83d14eb1e1a6fa6a1365483d99f4cefad1ea5dec204dad958e2a9a93add19781a8aa7bac71747b11d156711eafd1e873e19836eb573fa5cde284739df09b658ed40c56c7b5a7596840774a7065864e6c2af7b5a8bf7a2d238de83d77891d98ef5a4a58248c655a1c7c97c99e01d9928dc60c629eeb523356dc3686e3f9a1a30ffcd0268cd03718292f21d839fce741f4c1163001ab5b654c37d862998962a05e8028e061c611384772777ef6a49b00ebb4f228308e61b2afe408b33db2d82c4f385e26d7438ec0a183c64eeca4138cbc3dc2";
}
