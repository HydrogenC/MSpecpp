using System.IO;
using System.Linq;

namespace MSpecpp;

public class CaseFolder
{
    public string Dir { get; set; }

    public string DisplayName => Dir.Split(['/', '\\']).Last();

    public int SelectedCount { get; set; } = 0;
}