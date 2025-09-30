import { describe, it, expect, vi, beforeEach } from 'vitest';
import { HomeApiService } from '../homeApi';
import { apiClient } from '../../api/client';

vi.mock('../../api/client', () => ({
  apiClient: {
    post: vi.fn(),
  },
}));

describe('HomeApiService.recordInteraction', () => {
  const postMock = apiClient.post as unknown as ReturnType<typeof vi.fn>;

  beforeEach(() => {
    postMock.mockReset();
  });

  it('posts the complete interaction payload including timestamp', async () => {
    const interaction = {
      postId: 'post-1',
      interactionType: 'like' as const,
      duration: 12,
      timestamp: '2024-01-01T00:00:00.000Z',
    };

    await HomeApiService.recordInteraction(interaction);

    expect(postMock).toHaveBeenCalledWith('/api/home/interaction', interaction);
  });
});
