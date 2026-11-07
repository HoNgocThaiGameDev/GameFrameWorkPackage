#import <Foundation/Foundation.h>
#import <CoreHaptics/CoreHaptics.h>
#import "Haptic.h"

API_AVAILABLE(ios(13.0))
static CHHapticEngine *hapticEngine = nil;

NSMutableDictionary *hapticPatternsDict; // Dictionary to store haptic patterns.

extern "C"
{
    void _Initialize() 
	{
		if (@available(iOS 13.0, *)) 
		{
			NSError *error = nil;
			hapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];
			if (error) {
				NSLog(@"Error creating haptic engine: %@", error);
				return;
			}
			[hapticEngine startAndReturnError:&error];
			if (error) {
				NSLog(@"Error starting haptic engine: %@", error);
				return;
			}
		} 
		else 
		{
			NSLog(@"Haptic feedback not supported on this iOS version.");
		}
	}
	
	void _Play(float duration, float intensityValue)
	{
		if (@available(iOS 13.0, *)) // Check if the device is running iOS 13.0 or later, as Core Haptics is available only on iOS 13+.
		{  
			if (!hapticEngine) 
			{
				NSLog(@"Haptic engine is nil. Reinitializing...");
				
				_Initialize();
			} 
			else
			{
				NSError *startError = nil;
				[hapticEngine startAndReturnError:&startError];
				
				if (startError) {
					NSLog(@"Failed to restart haptic engine: %@", startError);
					return;
				}
			}
			
			NSError *error = nil;  // Declare an NSError object to capture any errors that may occur.

			// Create a haptic intensity parameter
			CHHapticEventParameter *intensity = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensityValue];

			// Create a haptic sharpness parameter
			CHHapticEventParameter *sharpness = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness value:0.0];

			// Create a continuous haptic event that uses the intensity and sharpness parameters.
			CHHapticEvent *event = [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticContinuous parameters:@[intensity, sharpness] relativeTime:0 duration:duration];

			// Create a haptic pattern using the event created above. This pattern can be played by the haptic engine.
			CHHapticPattern *pattern = [[CHHapticPattern alloc] initWithEvents:@[event] parameters:@[] error:&error];
			if (error) {  // Check if there was an error when creating the haptic pattern.
				NSLog(@"Error creating haptic pattern: %@", error);  // Log the error message.
				return;  // Exit the function if there was an error.
			}

			// Create a pattern player using the pattern created above. This player will handle playing the haptic pattern.
			id<CHHapticPatternPlayer> player = [hapticEngine createPlayerWithPattern:pattern error:&error];
			if (error) {  // Check if there was an error when creating the pattern player.
				NSLog(@"Error creating haptic player: %@", error);  // Log the error message.
				return;  // Exit the function if there was an error.
			}

			// Start playing the haptic pattern at time 0.
			[player startAtTime:0 error:&error];
			if (error) {  // Check if there was an error when starting the player.
				NSLog(@"Error playing haptic: %@", error);  // Log the error message.
				return;  // Exit the function if there was an error.
			}
		} 
		else 
		{
			NSLog(@"Haptic feedback not supported on this iOS version.");  // Log a message if the device is running an unsupported iOS version.
		}
	}
	
	void _RegisterPattern(const char* hapticPatternJson) 
	{
		if (@available(iOS 13.0, *)) 
		{
			if (!hapticEngine) 
			{
				NSLog(@"Haptic engine is nil. Reinitializing...");
				
				_Initialize();
			} 
			else
			{
				NSError *startError = nil;
				[hapticEngine startAndReturnError:&startError];
				
				if (startError) {
					NSLog(@"Failed to restart haptic engine: %@", startError);
					return;
				}
			}

			// Convert the C string to an NSString.
			NSString *jsonString = [NSString stringWithUTF8String:hapticPatternJson];

			// Convert the JSON string to a NSDictionary.
			NSData *jsonData = [jsonString dataUsingEncoding:NSUTF8StringEncoding];
			NSDictionary *hapticPatternDict = [NSJSONSerialization JSONObjectWithData:jsonData options:0 error:nil];

			// Extract the ID and pattern array.
			NSString *patternId = hapticPatternDict[@"ID"];
			NSArray *eventsArray = hapticPatternDict[@"Pattern"];
			NSMutableArray *events = [NSMutableArray array];

			// Iterate through the events array and create CHHapticEvent objects.
			for (NSDictionary *eventDict in eventsArray) {
				float intensityValue = [eventDict[@"Intensity"] floatValue];
				float sharpnessValue = [eventDict[@"Sharpness"] floatValue];
				float startTimeValue = [eventDict[@"StartTime"] floatValue];
				float durationValue = [eventDict[@"Duration"] floatValue];

				CHHapticEventParameter *intensity = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensityValue];
				CHHapticEventParameter *sharpness = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness value:sharpnessValue];

				CHHapticEvent *event = [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticContinuous
																	  parameters:@[intensity, sharpness]
																   relativeTime:startTimeValue
																	   duration:durationValue];

				[events addObject:event];
			}

			NSError *error = nil;
			CHHapticPattern *pattern = [[CHHapticPattern alloc] initWithEvents:events parameters:@[] error:&error];

			if (error) {
				NSLog(@"Error creating haptic pattern: %@", error);
				return;
			}

			// Initialize the dictionary if it's not already done.
			if (!hapticPatternsDict) {
				hapticPatternsDict = [NSMutableDictionary dictionary];
			}

			// Store the pattern in the dictionary using the ID as the key.
			hapticPatternsDict[patternId] = pattern;
		} 
		else 
		{
			NSLog(@"Haptic feedback not supported on this iOS version.");
		}
	}
	
	/// Play a custom haptic pattern from a JSON string.
	void _PlayPattern(const char* patternId)
	{
		if (@available(iOS 13.0, *)) 
		{
			if (!hapticEngine) 
			{
				NSLog(@"Haptic engine is nil. Reinitializing...");
				
				_Initialize();
			} 
			else
			{
				NSError *startError = nil;
				[hapticEngine startAndReturnError:&startError];
				
				if (startError) {
					NSLog(@"Failed to restart haptic engine: %@", startError);
					return;
				}
			}

			// Convert the C string patternId to an NSString.
			NSString *patternIdString = [NSString stringWithUTF8String:patternId];

			// Retrieve the pattern from the dictionary.
			CHHapticPattern *pattern = hapticPatternsDict[patternIdString];

			if (!pattern) {
				NSLog(@"Haptic pattern not found for ID: %@", patternIdString);
				return;
			}

			NSError *error = nil;

			// Create a pattern player using the retrieved pattern.
			id<CHHapticPatternPlayer> player = [hapticEngine createPlayerWithPattern:pattern error:&error];
			if (error) {
				NSLog(@"Error creating haptic player: %@", error);
				return;
			}

			// Start playing the haptic pattern at time 0.
			[player startAtTime:0 error:&error];
			if (error) {
				NSLog(@"Error playing haptic: %@", error);
			}
		} 
		else 
		{
			NSLog(@"Haptic feedback not supported on this iOS version.");
		}
	}
}
