using System.Collections.Generic;

namespace Emotional_Mapping.Web.Models;

public class HomeIndexViewModel
{
    public IReadOnlyList<string> HeroEmotionKeys { get; init; } = [];
}
