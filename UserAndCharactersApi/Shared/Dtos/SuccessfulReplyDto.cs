namespace UserWithCharacterVisibility.Dtos {

  public record SuccessfulReplyDto<T> 
    : ReplyBaseDto {
    public override bool Success
      => true;
    public T Result { get; init; }
  };

  public record SuccessfulReplyDto
    : ReplyBaseDto {
    public override bool Success
      => true;
  };
}
