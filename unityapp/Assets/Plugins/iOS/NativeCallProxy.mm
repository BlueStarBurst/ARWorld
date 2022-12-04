#import "NativeCallProxy.h"
#import <Foundation/Foundation.h>

@implementation FrameworkLibAPI

id<NativeCallsProtocol> api = NULL;
+ (void)registerAPIforNativeCalls:(id<NativeCallsProtocol>)aApi {
  api = aApi;
}

@end

/**
 * The methods below bridge the calls from Unity into iOS. When Unity call any
 * of the methods below, the call is forwarded to the iOS bridge using the
 * `NativeCallsProtocol`.
 */
extern "C" {

void sendUnityStateUpdate(const char *state) {
  const NSString *str = @(state);
  [api onUnityStateChange:str];
}

void saveMap(const char *map) {
  const NSString *str = @(map);
  [api onSaveMap:str];
}

void setTestDelegate(TestDelegate delegate) {
  [api onSetTestDelegate:delegate];
}

void setPhoneResponse(const char *response) {
  const NSString *str = @(response);
  [api onPhoneResponse:str];
}

// this method will take a byte array in C# and convert it to a NSData object
// void sendUnityDataUpdate(const char *data, int length) {
//   const NSData *nsData = [NSData dataWithBytes:data length:length];
//   [api onSaveARWorldMap:nsData];
// }
}
