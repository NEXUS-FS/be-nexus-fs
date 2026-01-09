using Application.DTOs.FileOperations;

namespace Application.UseCases.FileOperations.Commands
{
    public class MoveFileCommand
    {
        public MoveFileRequest Request { get; set; } = null!;
    }
}

