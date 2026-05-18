using FluentValidation;

namespace TeamFlow.Application.Comments.Dtos;

public record CreateCommentRequest(Guid TaskItemId, string Content, IReadOnlyList<Guid>? MentionedUserIds = null);
public record UpdateCommentRequest(string Content);

public record CommentDto(
    Guid Id,
    Guid TaskItemId,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.TaskItemId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.MentionedUserIds!)
            .Must(ids => ids == null || ids.Count <= 20)
            .WithMessage("At most 20 mentions per comment.")
            .When(x => x.MentionedUserIds != null);
    }
}

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
