using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.Templates;

public interface ITemplateService
{
    Task<string> RenderTemplateAsync(
        string templateName,
        Dictionary<string, object> data);
}
