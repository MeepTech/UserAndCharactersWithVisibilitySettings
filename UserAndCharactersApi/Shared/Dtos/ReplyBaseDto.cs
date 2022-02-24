using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserWithCharacterVisibility.Dtos {
  public abstract record ReplyBaseDto {
    public virtual bool Success { get; init; }
    public string Message { get; init; }
  };
}
