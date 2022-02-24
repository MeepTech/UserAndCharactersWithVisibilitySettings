namespace UserWithCharacterVisibility.Dtos {
  /// <summary>
  /// DTO for deleting a character
  /// </summary>
  public record DeleteCharacterDto {
    public string UniqueCharacterName { get; init; }
    public string Password { get; init; }
  }
}
