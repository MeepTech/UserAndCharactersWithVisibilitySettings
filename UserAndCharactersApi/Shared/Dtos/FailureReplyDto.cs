using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UserWithCharacterVisibility.Dtos {
  public record FailureReplyDto 
    : ReplyBaseDto 
  {

    public override bool Success
      => false;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<object> Errors {
      get; init;
    }
  }
}
