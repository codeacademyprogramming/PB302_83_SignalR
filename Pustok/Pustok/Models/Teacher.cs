namespace Pustok.Models
{
    public class Teacher:BaseEntity
    {
        public string FullName { get; set; }
        public List<TeacherSkill> TeacherSkills { get; set; }
    }
}
