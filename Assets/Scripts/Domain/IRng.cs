namespace Hellscape.Domain {
    public interface IRng {
        int Range(int minInclusive, int maxExclusive);
        float Next01();
    }
}
