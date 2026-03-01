public class Explosion
{
    public float CenterX;
    public float CenterY;

    public float CurrentRadius;
    public float MaxRadius;

    public float Speed;

    public Explosion(float centerX, float centerY, float maxRadius, float speed)
    {
        CenterX = centerX;
        CenterY = centerY;
        MaxRadius = maxRadius;
        Speed = speed;
    }
}