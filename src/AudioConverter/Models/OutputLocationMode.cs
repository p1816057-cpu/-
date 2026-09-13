namespace AudioConverter.Models
{
    /// <summary>
    /// 输出位置模式。
    /// ProgramFolder 为默认值：输出到程序自身目录（SkyFusion.exe 所在文件夹）下自动新建的
    /// 「转换输出」子文件夹；Custom 为用户显式选择的固定文件夹；SourceFolder 为输出到
    /// 每个源文件所在文件夹的「转换输出」子文件夹。
    /// 注意：ProgramFolder 必须保持为 0，旧版本配置文件缺少该字段时才能落到默认值。
    /// </summary>
    public enum OutputLocationMode
    {
        ProgramFolder = 0,
        Custom = 1,
        SourceFolder = 2
    }
}
