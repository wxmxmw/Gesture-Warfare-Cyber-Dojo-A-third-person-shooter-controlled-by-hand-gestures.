//卡尔曼滤波器 用于平滑手部坐标，减少抖动
using System;

namespace KalmanFilter
{
  public class KalmanFilter
  {
    private float _processNoiseCovariance = 1e-3f; // 过程噪声协方差
    private float _measurementNoiseCovariance = 1e-2f; // 测量噪声协方差
    private float _errorCovariancePost = 1.0f; // 后验误差协方差
    private float _stateEstimate = 0.0f; // 状态估计值

    public KalmanFilter()
    {
    }

    public KalmanFilter(float processNoiseCovariance, float measurementNoiseCovariance, float errorCovariancePost = 1.0f, float initialState = 0.0f)
    {
      _processNoiseCovariance = processNoiseCovariance;
      _measurementNoiseCovariance = measurementNoiseCovariance;
      _errorCovariancePost = errorCovariancePost;
      _stateEstimate = initialState;
    }
    
    public void Update(float measurement)
    {
      // 预测步骤
      float errorCovariancePrior = _errorCovariancePost + _processNoiseCovariance;

      // 更新步骤
      float kalmanGain = errorCovariancePrior / (errorCovariancePrior + _measurementNoiseCovariance);
      _stateEstimate += kalmanGain * (measurement - _stateEstimate);
      _errorCovariancePost = (1 - kalmanGain) * errorCovariancePrior;
    }

    public float GetState()
    {
      return _stateEstimate;
    }
  }
}
