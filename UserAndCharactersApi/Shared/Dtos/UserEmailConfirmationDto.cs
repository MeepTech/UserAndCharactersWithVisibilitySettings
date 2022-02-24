namespace UserWithCharacterVisibility.Dtos {
  public record UserEmailConfirmationDto {
    public string UserId { get; init; }
    public string Token { get; init; }
  }
}
