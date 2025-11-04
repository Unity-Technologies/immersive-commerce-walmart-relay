namespace Unity.WalmartAuthRelay.Interfaces;

public interface IMapperService
{
    public TDestination Map<TSource, TDestination>(TSource source);
}
