using Microsoft.AspNetCore.Components;
using Radzen;

namespace GitTransformer.Pages;

public partial class About
{
    [Inject] TooltipService TooltipService { get; set; } = null!;

    List<bool> _isExpanded = [
        false, false, false, false, false, false, false];
    string _icon = "stat_minus_2";

    void ShowTooltip(ElementReference elementReference, TooltipOptions options) => 
        TooltipService.Open(elementReference, _isExpanded[0] ? "Collapse All" : "Expand All", options);
}
