namespace UserWithCharacterVisibility.Dtos {
  /// <summary>
  /// DTO for creating a new user
  /// </summary>
  public record UserLoginDto : UsernamePasswordDto {
      public bool RememberMe { get; init; }
  };
}
